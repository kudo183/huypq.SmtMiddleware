using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using huypq.SmtShared;
using huypq.SmtShared.Constant;
using huypq.QueryBuilder;
using huypq.SmtMiddleware.Entities;
using Microsoft.Extensions.Logging;

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

        protected SmtActionResult SaveChanges()
        {
            try
            {
                var changeCount = DBContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.LogError(0, ex, "SaveChanges");
                return CreateObjectResult(null, System.Net.HttpStatusCode.InternalServerError);
            }
            //need return an json object, if just return status code, jquery will treat as fail.
            return CreateOKResult();
        }

        protected SmtActionResult SaveChanges(List<DtoType> items, List<EntityType> changedEntities)
        {
            try
            {
                var result = BeforeSave(items);
                var changeCount = DBContext.SaveChanges();
                if (result == null)
                {
                    AfterSave(items, changedEntities);
                }
                else
                {
                    AfterSaveWithBeforeSaveResult(items, changedEntities, result);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(0, ex, "SaveChanges");
                return CreateObjectResult(null, System.Net.HttpStatusCode.InternalServerError);
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
                var maxItemAllowed = GetMaxItemAllowed();
                if (filter.PageSize > 0 && filter.PageSize <= maxItemAllowed)
                {
                    if (filter.PageIndex <= 0)//forced to use paging
                    {
                        filter.PageIndex = 1;
                    }

                    if (filter.OrderOptions.Count == 0)
                    {
                        filter.OrderOptions.Add(GetDefaultOrderOption());
                    }
                    query = QueryExpression.AddQueryExpression(
                    query, ref filter, out pageCount);

                    result.PageIndex = filter.PageIndex;
                    result.PageCount = pageCount;
                }
                else
                {
                    var msg = string.Format("PageSize must greater than zero and lower than {0}", maxItemAllowed + 1);
                    return CreateObjectResult(msg, System.Net.HttpStatusCode.BadRequest);
                }
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
            if (IsSupportGetUpdate() == false)
            {
                return CreateObjectResult(null, System.Net.HttpStatusCode.NotImplemented);
            }

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
            var result = new PagingResultDto<DtoType>
            {
                Items = new List<DtoType>()
            };
            var tableID = GetTableID();

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

        protected int GetTableID()
        {
            if (IsSupportGetUpdate() == false)
                return 0;

            var tableName = GetTableName();
            var tableID = DBContext.SmtTable.FirstOrDefault(p => p.TableName == tableName).ID;
            return tableID;
        }

        protected SmtActionResult Save(List<DtoType> items)
        {
            List<EntityType> changedEntities = new List<EntityType>();

            var tableID = GetTableID();
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
                            AddSmtDeletedItemEntry(entity.ID, tableID, now);
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
            var now = DateTime.UtcNow.Ticks;
            dto.State = DtoState.Add;
            var entity = ConvertToEntity(dto);
            entity.TenantID = TokenModel.TenantID;
            entity.CreateTime = now;
            entity.LastUpdateTime = now;
            DBContext.Set<EntityType>().Add(entity);

            var result = SaveChanges(new List<DtoType>() { dto }, new List<EntityType>() { entity });
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return CreateObjectResult(entity.ID);
            }
            return result;
        }

        protected SmtActionResult Update(DtoType dto)
        {
            dto.State = DtoState.Update;
            var entity = ConvertToEntity(dto);
            entity.LastUpdateTime = DateTime.UtcNow.Ticks;

            if (entity.TenantID != TokenModel.TenantID)
            {
                return CreateObjectResult("wrong Tenant", System.Net.HttpStatusCode.BadRequest);
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
                return CreateObjectResult("wrong Tenant", System.Net.HttpStatusCode.BadRequest);
            }
            return Delete(dto, entity);
        }
        #endregion

        public abstract EntityType ConvertToEntity(DtoType dto);
        public abstract DtoType ConvertToDto(EntityType entity);

        protected virtual IQueryable<EntityType> GetQuery()
        {
            if (SmtSettings.Instance.SkipTenantFilterTables.Contains(GetTableName()) == true)
            {
                return DBContext.Set<EntityType>();
            }
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

        protected virtual OrderByExpression.OrderOption GetDefaultOrderOption()
        {
            return SmtSettings.Instance.DefaultOrderOption;
        }

        protected virtual void UpdateEntity(ContextType context, EntityType entity)
        {
            //mark all properties of entity as Modified, 
            context.Entry(entity).State = EntityState.Modified;
            //need set what [property] is unmodified
            //context.Entry(entity).Property(p => p.[property]).IsModified = false;

            //mark all properties of entity as Unchanged
            //context.Attach(entity);
            //need set what[property] is modified
            //context.Entry(entity).Property(p => p.[property]).IsModified = true;
        }

        protected SmtActionResult Delete(DtoType dto, EntityType entity)
        {
            var tableID = GetTableID();
            DBContext.Set<EntityType>().Remove(entity);
            var now = DateTime.UtcNow.Ticks;
            AddSmtDeletedItemEntry(entity.ID, tableID, now);
            return SaveChanges(new List<DtoType>() { dto }, new List<EntityType>() { entity });
        }

        protected SmtActionResult Delete(List<DtoType> dtos, List<EntityType> entities)
        {
            var tableID = GetTableID();
            DBContext.Set<EntityType>().RemoveRange(entities);
            var now = DateTime.UtcNow.Ticks;
            foreach (var entity in entities)
            {
                AddSmtDeletedItemEntry(entity.ID, tableID, now);
            }
            return SaveChanges(dtos, entities);
        }

        protected virtual void AfterSave(List<DtoType> items, List<EntityType> changedEntities)
        {

        }

        protected virtual void AfterSaveWithBeforeSaveResult(List<DtoType> items, List<EntityType> changedEntities, object beforeSaveResult)
        {

        }

        protected virtual object BeforeSave(List<DtoType> items)
        {
            return null;
        }

        protected virtual bool IsSupportGetUpdate()
        {
            return false;
        }

        private void AddSmtDeletedItemEntry(int entityID, int tableID, long now)
        {
            if (IsSupportGetUpdate() == false)
            {
                return;
            }

            DBContext.SmtDeletedItem.Add(new SmtDeletedItem()
            {
                TenantID = TokenModel.TenantID,
                DeletedID = entityID,
                TableID = tableID,
                CreateTime = now
            });
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
