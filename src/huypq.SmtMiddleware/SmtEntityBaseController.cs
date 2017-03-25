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
        #region define class
        [ProtoBuf.ProtoContract]
        public class PagingResultDto<T>
        {
            [ProtoBuf.ProtoMember(1)]
            public int TotalItemCount { get; set; }
            [ProtoBuf.ProtoMember(2)]
            public int PageIndex { get; set; }
            [ProtoBuf.ProtoMember(3)]
            public int PageCount { get; set; }
            [ProtoBuf.ProtoMember(4)]
            public List<T> Items { get; set; }
            [ProtoBuf.ProtoMember(5)]
            public long VersionNumber { get; set; }
            [ProtoBuf.ProtoMember(6)]
            public string ErrorMsg { get; set; }
            [ProtoBuf.ProtoMember(7)]
            public long ServerStartTime { get; set; }
            [ProtoBuf.ProtoMember(8)]
            public int PageSize { get; set; }
        }

        public class ChangeState
        {
            public const int Original = 0;
            public const int Add = 1;
            public const int Delete = 2;
            public const int Update = 3;
        }

        #endregion

        private static object _versionNumberLock = new object();

        private static Dictionary<int, long> VersionNumbers = new Dictionary<int, long>();

        public static void IncreaseVersionNumber(int groupId)
        {
            lock (_versionNumberLock)
            {
                long versionNumber;
                if (VersionNumbers.TryGetValue(groupId, out versionNumber) == false)
                {
                    VersionNumbers.Add(groupId, 1);
                }

                VersionNumbers[groupId] = versionNumber + 1;
            }
        }

        private long GetVersionNumber()
        {
            long versionNumber;
            if (VersionNumbers.TryGetValue(TokenModel.TenantId, out versionNumber) == false)
            {
                return 0;
            }

            return versionNumber;
        }

        private ContextType _context;
        protected ContextType DBContext
        {
            get
            {
                return _context;
            }
        }

        protected SmtActionResult SaveChanges()
        {
            try
            {
                var changeCount = DBContext.SaveChanges();
                if (changeCount > 0)
                {
                    IncreaseVersionNumber(TokenModel.TenantId);
                }
                AfterSave();
            }
            catch (Exception ex)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.InternalServerError);
            }
            //need return an json object, if just return status code, jquery will treat as fail.
            return CreateObjectResult("OK");
        }

        public override void Init(SmtTokenModel token, IApplicationBuilder app, HttpContext context, string requestType)
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

            result.VersionNumber = GetVersionNumber();
            result.ServerStartTime = SmtSettings.ServerStartTime;
            var pageSize = GetPageSize();

            if (filter != null)
            {
                if (result.ServerStartTime == filter.ServerStartTime
                    && result.VersionNumber == filter.VersionNumber)
                {
                    return CreateObjectResult(result);
                }

                if (filter.PageIndex > 0)
                {
                    if (filter.PageSize > pageSize)
                    {
                        filter.PageSize = pageSize;
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
            foreach (var dto in items)
            {
                var entity = ConvertToEntity(dto);

                switch (dto.State)
                {
                    case ChangeState.Add:
                        entity.TenantID = TokenModel.TenantId;
                        DBContext.Set<EntityType>().Add(entity);
                        break;
                    case ChangeState.Update:
                        if (entity.TenantID == TokenModel.TenantId)
                        {
                            UpdateEntity(DBContext, entity);
                        }
                        break;
                    case ChangeState.Delete:
                        if (entity.TenantID == TokenModel.TenantId)
                        {
                            DBContext.Set<EntityType>().Remove(entity);
                        }
                        break;
                    default:
                        return CreateStatusResult(System.Net.HttpStatusCode.InternalServerError);
                }
            }

            return SaveChanges();
        }

        protected SmtActionResult Add(DtoType dto)
        {
            var entity = ConvertToEntity(dto);
            entity.TenantID = TokenModel.TenantId;

            DBContext.Set<EntityType>().Add(entity);

            return SaveChanges();
        }

        protected SmtActionResult Update(DtoType dto)
        {
            var entity = ConvertToEntity(dto);
            if (entity.TenantID != TokenModel.TenantId)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            UpdateEntity(DBContext, entity);

            return SaveChanges();
        }

        protected SmtActionResult Delete(DtoType dto)
        {
            var entity = ConvertToEntity(dto);
            if (entity.TenantID != TokenModel.TenantId)
            {
                return CreateStatusResult(System.Net.HttpStatusCode.Unauthorized);
            }

            DBContext.Set<EntityType>().Remove(entity);

            return SaveChanges();
        }
        #endregion
        
        public abstract EntityType ConvertToEntity(DtoType dto);
        public abstract DtoType ConvertToDto(EntityType entity);

        protected virtual IQueryable<EntityType> GetQuery()
        {
            return DBContext.Set<EntityType>().Where(p => p.TenantID == TokenModel.TenantId);
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

        protected virtual void AfterSave()
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
