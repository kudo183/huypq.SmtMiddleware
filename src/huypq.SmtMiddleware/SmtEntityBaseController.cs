using huypq.SmtShared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QueryBuilder;
using System;
using System.Collections.Generic;
using System.Linq;

namespace huypq.SmtMiddleware
{
    public abstract class SmtEntityBaseController<ContextType, EntityType, DtoType> : SmtAbstractController, IDisposable
        where ContextType : DbContext
        where EntityType : class, SmtIEntity
        where DtoType : class, SmtIDto
    {
        private ContextType _context;
        protected ContextType DBContext
        {
            get
            {
                return _context;
            }
        }

        protected SmtActionResult SaveChanges(List<DtoType> items, List<EntityType> changedEntities)
        {
            try
            {
                var changeCount = DBContext.SaveChanges();
                AfterSave(items, changedEntities);
            }
            catch (Exception ex)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.InternalServerError);
            }
            //need return an json object, if just return status code, jquery will treat as fail.
            return CreateOKResult();
        }

        public override void Init(TokenManager.LoginToken token, IApplicationBuilder app, HttpContext context, string requestType)
        {
            base.Init(token, app, context, requestType);
            _context = (ContextType)Context.RequestServices.GetService(typeof(ContextType));
        }

        #region action
        public override SmtActionResult ActionInvoker(string actionName, Dictionary<string, object> parameter)
        {
            SmtActionResult result = null;

            switch (actionName)
            {
                case "get":
                    result = Get(ConvertRequestBody<QueryExpression>(parameter["body"] as System.IO.Stream), GetQuery());
                    break;
                case "getbyid":
                    result = GetByID(int.Parse(parameter["id"].ToString()), GetQuery());
                    break;
                case "save":
                    result = Save(ConvertRequestBody<List<DtoType>>(parameter["body"] as System.IO.Stream));
                    break;
                case "add":
                    result = Add(ConvertRequestBody<DtoType>(parameter["body"] as System.IO.Stream));
                    break;
                case "update":
                    result = Update(ConvertRequestBody<DtoType>(parameter["body"] as System.IO.Stream));
                    break;
                case "delete":
                    result = Delete(ConvertRequestBody<DtoType>(parameter["body"] as System.IO.Stream));
                    break;
                default:
                    break;
            }

            return result;
        }

        protected SmtActionResult Get(QueryExpression filter, IQueryable<EntityType> includedQuery)
        {
            int pageCount = 1;
            var query = includedQuery;
            var result = new PagingResultDto<DtoType>
            {
                Items = new List<DtoType>()
            };

            var pageSize = GetPageSize();

            if (filter != null)
            {
                if (filter.PageIndex > 0)
                {
                    if (filter.PageSize > pageSize)
                    {
                        filter.PageSize = pageSize;
                    }
                    if (filter.OrderOptions.Count == 0)
                    {
                        filter.OrderOptions.Add(SmtSettings.Instance.DefaultOrderOption);
                    }
                    query = QueryExpression.AddQueryExpression(
                    query, ref filter, out pageCount);

                    result.PageIndex = filter.PageIndex;
                    result.PageSize = filter.PageSize;
                    result.PageCount = pageCount;
                }
                else
                {
                    query = WhereExpression.AddWhereExpression(query, filter.WhereOptions);
                    query = OrderByExpression.AddOrderByExpression(query, filter.OrderOptions);
                }
            }

            var itemCount = query.Count();
            var maxItem = GetMaxItemAllowed();

            if (itemCount > maxItem)
            {
                result.ErrorMsg = "Entity set too large, please use paging";
                return CreateObjectResult(result);
            }

            result.LastUpdateTime = DateTime.UtcNow.Ticks;
            foreach (var entity in query)
            {
                result.Items.Add(ConvertToDto(entity));
            }

            return CreateObjectResult(result);
        }

        protected SmtActionResult GetByID(int id, IQueryable<EntityType> includedQuery)
        {
            var entity = includedQuery.FirstOrDefault(p => p.ID == id);
            return CreateObjectResult(ConvertToDto(entity));
        }

        protected SmtActionResult Save(List<DtoType> items)
        {
            List<EntityType> changedEntities = new List<EntityType>();
            foreach (var dto in items)
            {
                var entity = ConvertToEntity(dto);
                entity.LastUpdateTime = DateTime.UtcNow.Ticks;
                switch (dto.State)
                {
                    case DtoState.Add:
                        entity.TenantID = TokenModel.TenantID;
                        DBContext.Set<EntityType>().Add(entity);
                        changedEntities.Add(entity);
                        break;
                    case DtoState.Update:
                        if (entity.TenantID == TokenModel.TenantID)
                        {
                            UpdateEntity(DBContext, entity);
                            changedEntities.Add(entity);
                        }
                        break;
                    case DtoState.Delete:
                        if (entity.TenantID == TokenModel.TenantID)
                        {
                            DBContext.Set<EntityType>().Remove(entity);
                            changedEntities.Add(entity);
                        }
                        break;
                    default:
                        return CreateStatusResult(System.Net.HttpStatusCode.InternalServerError);
                }
            }

            return SaveChanges(items, changedEntities);
        }

        protected SmtActionResult Add(DtoType dto)
        {
            dto.State = DtoState.Add;
            var entity = ConvertToEntity(dto);
            entity.TenantID = TokenModel.TenantID;
            entity.LastUpdateTime = DateTime.UtcNow.Ticks;
            DBContext.Set<EntityType>().Add(entity);

            return SaveChanges(new List<DtoType>() { dto }, new List<EntityType>() { entity });
        }

        protected SmtActionResult Update(DtoType dto)
        {
            dto.State = DtoState.Update;
            var entity = ConvertToEntity(dto);
            entity.LastUpdateTime = DateTime.UtcNow.Ticks;
            if (entity.TenantID != TokenModel.TenantID)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            UpdateEntity(DBContext, entity);

            return SaveChanges(new List<DtoType>() { dto }, new List<EntityType>() { entity });
        }

        protected SmtActionResult Delete(DtoType dto)
        {
            dto.State = DtoState.Delete;
            var entity = ConvertToEntity(dto);
            if (entity.TenantID != TokenModel.TenantID)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            DBContext.Set<EntityType>().Remove(entity);

            return SaveChanges(new List<DtoType>() { dto }, new List<EntityType>() { entity });
        }
        #endregion

        public abstract EntityType ConvertToEntity(DtoType dto);
        public abstract DtoType ConvertToDto(EntityType entity);

        protected virtual IQueryable<EntityType> GetQuery()
        {
            return DBContext.Set<EntityType>().Where(p => p.TenantID == TokenModel.TenantID);
        }

        protected virtual int GetMaxItemAllowed()
        {
            return SmtSettings.Instance.MaxItemAllowed;
        }

        protected virtual int GetPageSize()
        {
            return SmtSettings.Instance.DefaultPageSize;
        }

        protected virtual DataType ConvertRequestBody<DataType>(System.IO.Stream requestBody)
        {
            DataType data = default(DataType);
            switch (RequestObjectType)
            {
                case "json":
                    data = SmtSettings.Instance.JsonSerializer.Deserialize<DataType>(requestBody);
                    break;
                case "protobuf":
                    data = SmtSettings.Instance.BinarySerializer.Deserialize<DataType>(requestBody);
                    break;
            }

            return data;
        }

        protected virtual void UpdateEntity(ContextType context, EntityType entity)
        {
            context.Entry(entity).State = EntityState.Modified;
        }

        protected virtual void AfterSave(List<DtoType> items, List<EntityType> changedEntities)
        {

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
