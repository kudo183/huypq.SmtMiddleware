using huypq.SmtShared;
using huypq.SmtShared.Constant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace huypq.SmtMiddleware
{
    public abstract class SmtFileBaseController<ContextType, EntityType> : SmtAbstractController, IDisposable
        where ContextType : DbContext, IDbContext
        where EntityType : class, IFileEntity, new()
    {
        private ContextType _context;
        protected ContextType DBContext
        {
            get
            {
                return _context;
            }
        }

        public override void Init(TokenManager.LoginToken token, IApplicationBuilder app, HttpContext context, string requestType)
        {
            base.Init(token, app, context, requestType);
            _context = (ContextType)Context.RequestServices.GetService(typeof(ContextType));
        }

        public override SmtActionResult ActionInvoker(string actionName, Dictionary<string, object> parameter)
        {
            SmtActionResult result = null;

            switch (actionName)
            {
                case ControllerAction.SmtFileBase.GetByID:
                    result = GetByID(int.Parse(parameter["id"].ToString()), GetQuery());
                    break;
                case ControllerAction.SmtFileBase.Download:
                    result = Download(int.Parse(parameter["id"].ToString()), GetQuery());
                    break;
                case ControllerAction.SmtFileBase.Add:
                    result = Add(parameter["file"] as IFormFile);
                    break;
                case ControllerAction.SmtFileBase.Update:
                    result = Update(int.Parse(parameter["id"].ToString()), parameter["file"] as IFormFile, GetQuery());
                    break;
                case ControllerAction.SmtFileBase.Delete:
                    result = Delete(int.Parse(parameter["id"].ToString()), GetQuery());
                    break;
                default:
                    break;
            }

            return result;
        }

        protected virtual SmtActionResult GetByID(int id, IQueryable<EntityType> query)
        {
            var entity = query.FirstOrDefault(p => p.ID == id);
            var stream = new FileStream(GetFilePath(entity), FileMode.Open);
            return CreateStreamResult(stream, entity.MimeType);
        }

        protected virtual SmtActionResult Delete(int id, IQueryable<EntityType> query)
        {
            var entity = query.FirstOrDefault(p => p.ID == id);
            if (entity == null)
            {
                return CreateObjectResult(string.Format("{0} not found", id), System.Net.HttpStatusCode.NotFound);
            }

            var filePath = GetFilePath(entity);
            File.Delete(filePath);

            DBContext.Set<EntityType>().Remove(entity);
            DBContext.SaveChanges();

            return CreateOKResult();
        }

        protected virtual SmtActionResult Download(int id, IQueryable<EntityType> query)
        {
            var entity = query.FirstOrDefault(p => p.ID == id);
            var stream = new FileStream(GetFilePath(entity), FileMode.Open);
            return CreateFileResult(stream, entity.FileName);
        }

        protected virtual SmtActionResult Add(IFormFile file)
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

                using (var fileStream = new FileStream(GetFilePath(entity), FileMode.Create))
                {
                    //await file.CopyToAsync(fileStream);
                    file.CopyTo(fileStream);
                }

                return CreateObjectResult(entity.ID.ToString());
            }

            return CreateObjectResult("file length = 0", System.Net.HttpStatusCode.BadRequest);
        }

        protected virtual SmtActionResult Update(int id, IFormFile file, IQueryable<EntityType> query)
        {
            var entity = query.FirstOrDefault(p => p.ID == id);
            if (entity == null)
            {
                return CreateObjectResult(string.Format("{0} not found", id), System.Net.HttpStatusCode.NotFound);
            }

            if (file.Length > 0)
            {
                var p = GetFilePath(entity);
                File.Delete(p);
                entity.LastUpdateTime = DateTime.UtcNow.Ticks;
                entity.FileName = file.FileName;
                entity.FileSize = file.Length;
                entity.MimeType = file.ContentType;

                DBContext.Set<EntityType>().Update(entity);
                DBContext.SaveChanges();

                using (var fileStream = new FileStream(GetFilePath(entity), FileMode.Create))
                {
                    //await file.CopyToAsync(fileStream);
                    file.CopyTo(fileStream);
                }

                return CreateOKResult();
            }

            return CreateObjectResult("file length = 0", System.Net.HttpStatusCode.BadRequest);
        }

        protected virtual string GetFilePath(EntityType entity)
        {
            string dir = System.IO.Path.Combine(SmtSettings.Instance.SmtFileDirectoryPath, entity.TenantID.ToString());
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return System.IO.Path.Combine(dir, string.Format("{0:D9}_{1}", entity.ID, entity.FileName));
        }

        protected virtual IQueryable<EntityType> GetQuery()
        {
            return DBContext.Set<EntityType>().Where(p => p.TenantID == TokenModel.TenantID);
        }

        public void Dispose()
        {
            if (DBContext != null)
            {
                DBContext.Dispose();
            }
        }
    }
}
