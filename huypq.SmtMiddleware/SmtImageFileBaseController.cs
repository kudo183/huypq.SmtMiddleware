using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Microsoft.AspNetCore.Http;
using huypq.SmtShared.Constant;
using System.Linq;
using System.Drawing.Imaging;

namespace huypq.SmtMiddleware
{
    public abstract class SmtImageFileBaseController<ContextType, EntityType> : SmtFileBaseController<ContextType, EntityType>
        where ContextType : DbContext, IDbContext
        where EntityType : class, IFileEntity, new()
    {
        public override SmtActionResult ActionInvoker(string actionName, Dictionary<string, object> parameter)
        {
            SmtActionResult result = null;

            switch (actionName)
            {
                case ControllerAction.SmtImageFileBase.GetThumbnailByID:
                    result = GetThumbnailByID(int.Parse(parameter["id"].ToString()), GetQuery());
                    break;
                default:
                    result = base.ActionInvoker(actionName, parameter);
                    break;
            }

            return result;
        }

        protected virtual SmtActionResult GetThumbnailByID(int id, IQueryable<EntityType> query)
        {
            var entity = query.FirstOrDefault(p => p.ID == id);
            var stream = new FileStream(GetThumbnailFilePath(GetFilePath(entity)), FileMode.Open);
            return CreateStreamResult(stream, entity.MimeType);
        }

        protected override SmtActionResult Delete(int id, IQueryable<EntityType> query)
        {
            var entity = query.FirstOrDefault(p => p.ID == id);
            if (entity == null)
            {
                return CreateObjectResult(string.Format("{0} not found", id), System.Net.HttpStatusCode.NotFound);
            }

            var filePath = GetFilePath(entity);
            File.Delete(filePath);
            File.Delete(GetThumbnailFilePath(filePath));

            DBContext.Set<EntityType>().Remove(entity);
            DBContext.SaveChanges();

            return CreateOKResult();
        }

        protected override SmtActionResult Add(IFormFile file)
        {
            if (file.Length > 0)
            {
                var now = DateTime.UtcNow.Ticks;
                var entity = new EntityType()
                {
                    TenantID = TokenModel.TenantID,
                    CreateTime = now,
                    LastUpdateTime = now,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    MimeType = file.ContentType
                };
                DBContext.Set<EntityType>().Add(entity);
                DBContext.SaveChanges();

                var filePath = GetFilePath(entity);
                using (var fileStream = new FileStream(GetFilePath(entity), FileMode.Create))
                {
                    //await file.CopyToAsync(fileStream);
                    file.CopyTo(fileStream);
                }

                using (var fileStream = new FileStream(GetThumbnailFilePath(filePath), FileMode.Create))
                using (Image img = Resize(Image.FromStream(file.OpenReadStream()), 100, 100))
                {
                    switch (entity.MimeType)
                    {
                        case "image/jpeg":
                            img.Save(fileStream, ImageFormat.Jpeg);
                            break;
                        case "image/png":
                            img.Save(fileStream, ImageFormat.Png);
                            break;
                        default:
                            img.Save(fileStream, img.RawFormat);
                            break;
                    }
                }

                return CreateObjectResult(entity.ID.ToString());
            }

            return CreateObjectResult("file length = 0", System.Net.HttpStatusCode.BadRequest);
        }

        protected override SmtActionResult Update(int id, IFormFile file, IQueryable<EntityType> query)
        {
            var entity = query.FirstOrDefault(p => p.ID == id);
            if (entity == null)
            {
                return CreateObjectResult(string.Format("{0} not found", id), System.Net.HttpStatusCode.NotFound);
            }

            if (file.Length > 0)
            {
                var oldFilePath = GetFilePath(entity);
                File.Delete(oldFilePath);
                File.Delete(GetThumbnailFilePath(oldFilePath));

                entity.LastUpdateTime = DateTime.UtcNow.Ticks;
                entity.FileName = file.FileName;
                entity.FileSize = file.Length;
                entity.MimeType = file.ContentType;

                DBContext.Set<EntityType>().Update(entity);
                DBContext.SaveChanges();

                var filePath = GetFilePath(entity);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    //await file.CopyToAsync(fileStream);
                    file.CopyTo(fileStream);
                }

                using (var fileStream = new FileStream(GetThumbnailFilePath(filePath), FileMode.Create))
                using (Image img = Resize(Image.FromStream(file.OpenReadStream()), 100, 100))
                {
                    switch (entity.MimeType)
                    {
                        case "image/jpeg":
                            img.Save(fileStream, ImageFormat.Jpeg);
                            break;
                        case "image/png":
                            img.Save(fileStream, ImageFormat.Png);
                            break;
                        default:
                            img.Save(fileStream, img.RawFormat);
                            break;
                    }
                }

                return CreateOKResult();
            }

            return CreateObjectResult("file length = 0", System.Net.HttpStatusCode.BadRequest);
        }

        protected override string GetFilePath(EntityType entity)
        {
            string dir = System.IO.Path.Combine(SmtSettings.Instance.SmtFileDirectoryPath, entity.TenantID.ToString(), "Image");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return System.IO.Path.Combine(dir, string.Format("{0:D9}_{1}", entity.ID, entity.FileName));
        }

        private string GetThumbnailFilePath(string filePath)
        {
            var thumbnailFileName = string.Format("{0}_thumb{1}", System.IO.Path.GetFileNameWithoutExtension(filePath), System.IO.Path.GetExtension(filePath));
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath), thumbnailFileName);
        }

        #region https://code.msdn.microsoft.com/How-to-resize-image-after-c8fce9b4
        private Image Resize(Image current, int maxWidth, int maxHeight)
        {
            int width, height;
            #region reckon size 
            if (current.Width > current.Height)
            {
                width = maxWidth;
                height = Convert.ToInt32(current.Height * maxHeight / (double)current.Width);
            }
            else
            {
                width = Convert.ToInt32(current.Width * maxWidth / (double)current.Height);
                height = maxHeight;
            }
            #endregion

            #region get resized bitmap 
            var canvas = new Bitmap(width, height);

            using (var graphics = Graphics.FromImage(canvas))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(current, 0, 0, width, height);
            }

            return canvas;
            #endregion
        }

        private byte[] ToByteArray(Image current, string mimeType)
        {
            using (var stream = new MemoryStream())
            {
                switch (mimeType)
                {
                    case "image/jpeg":
                        current.Save(stream, ImageFormat.Jpeg);
                        break;
                    case "image/png":
                        current.Save(stream, ImageFormat.Png);
                        break;
                    default:
                        current.Save(stream, current.RawFormat);
                        break;
                }
                return stream.ToArray();
            }
        }
        #endregion
    }
}
