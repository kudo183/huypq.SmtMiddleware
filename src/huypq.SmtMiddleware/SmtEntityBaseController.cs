using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using huypq.SmtShared;
using huypq.SmtShared.Constant;
using QueryBuilder;
using huypq.SmtMiddleware.Entities;

namespace huypq.SmtMiddleware
{
    public abstract class SmtEntityBaseController<ContextType, EntityType, DtoType> : SmtAbstractController, IDisposable
        where ContextType : DbContext, IDbContext
        where EntityType : class, IEntity
        where DtoType : class, IDto, new()
    {
        private ContextType _context;
        protected ContextType DBContext
        {
            get
            {
                return _context;
            }
        }

        protected string GetTableName()
        {
            return typeof(EntityType).Name;
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
                return CreateObjectResult(ex.Message, System.Net.HttpStatusCode.InternalServerError);
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
                case ControllerAction.SmtEntityBase.Get:
                    result = Get(ConvertRequestBody<QueryExpression>(parameter["body"] as System.IO.Stream), GetQuery());
                    break;
                case ControllerAction.SmtEntityBase.GetByID:
                    result = GetByID(int.Parse(parameter["id"].ToString()), GetQuery());
                    break;
                case ControllerAction.SmtEntityBase.GetAll:
                    result = GetAll(ConvertRequestBody<QueryExpression>(parameter["body"] as System.IO.Stream), GetQuery());
                    break;
                case ControllerAction.SmtEntityBase.GetUpdate:
                    result = GetUpdate(ConvertRequestBody<QueryExpression>(parameter["body"] as System.IO.Stream), GetQuery());
                    break;
                case ControllerAction.SmtEntityBase.Save:
                    result = Save(ConvertRequestBody<List<DtoType>>(parameter["body"] as System.IO.Stream));
                    break;
                case ControllerAction.SmtEntityBase.Add:
                    result = Add(ConvertRequestBody<DtoType>(parameter["body"] as System.IO.Stream));
                    break;
                case ControllerAction.SmtEntityBase.Update:
                    result = Update(ConvertRequestBody<DtoType>(parameter["body"] as System.IO.Stream));
                    break;
                case ControllerAction.SmtEntityBase.Delete:
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

            if (filter != null)
            {
                if (filter.PageIndex > 0)
                {
                    if (filter.OrderOptions.Count == 0)
                    {
                        filter.OrderOptions.Add(SmtSettings.Instance.DefaultOrderOption);
                    }
                    query = QueryExpression.AddQueryExpression(
                    query, ref filter, out pageCount);

                    result.PageIndex = filter.PageIndex;
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
                var msg = string.Format("Entity set too large, max item allowed is {0}", maxItem);
                return CreateObjectResult(msg, System.Net.HttpStatusCode.BadRequest);
            }

            result.LastUpdateTime = DateTime.UtcNow.Ticks;
            foreach (var entity in query)
            {
                if (entity.LastUpdateTime <= result.LastUpdateTime)
                {
                    result.Items.Add(ConvertToDto(entity));
                }
            }

            return CreateObjectResult(result);
        }

        protected SmtActionResult GetAll(QueryExpression filter, IQueryable<EntityType> includedQuery)
        {
            var query = includedQuery;
            var result = new PagingResultDto<DtoType>
            {
                Items = new List<DtoType>()
            };

            if (filter != null)
            {
                query = WhereExpression.AddWhereExpression(query, filter.WhereOptions);
                //query = OrderByExpression.AddOrderByExpression(query, filter.OrderOptions); //must order in client for perfomance
            }

            result.LastUpdateTime = DateTime.UtcNow.Ticks;
            foreach (var entity in query)
            {
                if (entity.LastUpdateTime <= result.LastUpdateTime)
                {
                    result.Items.Add(ConvertToDto(entity));
                }
            }

            return CreateObjectResult(result);
        }

        protected SmtActionResult GetUpdate(QueryExpression filter, IQueryable<EntityType> includedQuery)
        {
            if (filter == null)
            {
                var msg = string.Format(string.Format("Need specify {0} where options", nameof(IDto.LastUpdateTime)));
                return CreateObjectResult(msg, System.Net.HttpStatusCode.BadRequest);
            }

            var lastUpdateWhereOption = filter.WhereOptions.Find(p => p.PropertyPath == nameof(IDto.LastUpdateTime));
            if (lastUpdateWhereOption == null)
            {
                var msg = string.Format(string.Format("Need specify {0} where options", nameof(IDto.LastUpdateTime)));
                return CreateObjectResult(msg, System.Net.HttpStatusCode.BadRequest);
            }

            var query = includedQuery;
            var tableName = GetTableName();
            var result = new PagingResultDto<DtoType>
            {
                Items = new List<DtoType>()
            };
            var tableID = DBContext.SmtTable.FirstOrDefault(p => p.TableName == tableName).ID;

            query = WhereExpression.AddWhereExpression(query, filter.WhereOptions);
            //query = OrderByExpression.AddOrderByExpression(query, filter.OrderOptions); //must order in client for perfomance

            var lastUpdate = (long)lastUpdateWhereOption.GetValue();
            var deletedItemsQuery = DBContext.SmtDeletedItem.Where(p => p.TenantID == TokenModel.TenantID && p.TableID == tableID && p.CreateTime > lastUpdate);

            var itemCount = query.Count() + deletedItemsQuery.Count();
            var maxItem = GetMaxItemAllowed();

            if (itemCount > maxItem)
            {
                var msg = string.Format("Entity set too large, max item allowed is {0}", maxItem);
                return CreateObjectResult(msg, System.Net.HttpStatusCode.BadRequest);
            }

            result.LastUpdateTime = DateTime.UtcNow.Ticks;
            foreach (var entity in query)
            {
                if (entity.LastUpdateTime <= result.LastUpdateTime)
                {
                    var dto = ConvertToDto(entity);
                    dto.State = (dto.CreateTime > lastUpdate) ? DtoState.Add : DtoState.Update;
                    result.Items.Add(dto);
                }
            }
            foreach (var item in deletedItemsQuery)
            {
                if (item.CreateTime <= result.LastUpdateTime)
                {
                    result.Items.Add(new DtoType() { ID = item.DeletedID, State = DtoState.Delete, CreateTime = item.CreateTime });
                }
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
            var tableName = GetTableName();
            var tableID = DBContext.SmtTable.FirstOrDefault(p => p.TableName == tableName).ID;
            var now = DateTime.UtcNow.Ticks;

            foreach (var dto in items)
            {
                var entity = ConvertToEntity(dto);

                switch (dto.State)
                {
                    case DtoState.Add:
                        entity.TenantID = TokenModel.TenantID;
                        entity.CreateTime = now;
                        entity.LastUpdateTime = now;
                        DBContext.Set<EntityType>().Add(entity);
                        changedEntities.Add(entity);
                        break;
                    case DtoState.Update:
                        if (entity.TenantID == TokenModel.TenantID)
                        {
                            entity.LastUpdateTime = now;
                            UpdateEntity(DBContext, entity);
                            changedEntities.Add(entity);
                        }
                        break;
                    case DtoState.Delete:
                        if (entity.TenantID == TokenModel.TenantID)
                        {
                            DBContext.Set<EntityType>().Remove(entity);
                            DBContext.SmtDeletedItem.Add(new SmtDeletedItem()
                            {
                                TenantID = TokenModel.TenantID,
                                DeletedID = entity.ID,
                                TableID = tableID,
                                CreateTime = now
                            });
                            changedEntities.Add(entity);
                        }
                        break;
                    default:
                        return CreateObjectResult("invalid dto State", System.Net.HttpStatusCode.BadRequest);
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
                return CreateObjectResult("wrong Tenant", System.Net.HttpStatusCode.Unauthorized);
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
                return CreateObjectResult("wrong Tenant", System.Net.HttpStatusCode.Unauthorized);
            }

            var tableName = GetTableName();
            DBContext.Set<EntityType>().Remove(entity);
            DBContext.SmtDeletedItem.Add(new SmtDeletedItem()
            {
                TenantID = TokenModel.TenantID,
                DeletedID = entity.ID,
                TableID = DBContext.SmtTable.FirstOrDefault(p => p.TableName == tableName).ID,
                CreateTime = DateTime.UtcNow.Ticks
            });
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

        protected virtual DataType ConvertRequestBody<DataType>(System.IO.Stream requestBody)
        {
            DataType data = default(DataType);
            switch (RequestObjectType)
            {
                case SerializeType.Json:
                    data = SmtSettings.Instance.JsonSerializer.Deserialize<DataType>(requestBody);
                    break;
                case SerializeType.Protobuf:
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
