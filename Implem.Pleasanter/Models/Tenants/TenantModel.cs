﻿using Implem.DefinitionAccessor;
using Implem.Libraries.Classes;
using Implem.Libraries.DataSources.SqlServer;
using Implem.Libraries.Utilities;
using Implem.Pleasanter.Libraries.DataSources;
using Implem.Pleasanter.Libraries.DataTypes;
using Implem.Pleasanter.Libraries.General;
using Implem.Pleasanter.Libraries.Html;
using Implem.Pleasanter.Libraries.HtmlParts;
using Implem.Pleasanter.Libraries.Models;
using Implem.Pleasanter.Libraries.Requests;
using Implem.Pleasanter.Libraries.Responses;
using Implem.Pleasanter.Libraries.Security;
using Implem.Pleasanter.Libraries.Server;
using Implem.Pleasanter.Libraries.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace Implem.Pleasanter.Models
{
    public class TenantModel : BaseModel
    {
        public int TenantId = Sessions.TenantId();
        public string TenantName = string.Empty;
        public Title Title = new Title();
        public string Body = string.Empty;
        public int SavedTenantId = Sessions.TenantId();
        public string SavedTenantName = string.Empty;
        public string SavedTitle = string.Empty;
        public string SavedBody = string.Empty;
        public bool TenantId_Updated { get { return TenantId != SavedTenantId; } }
        public bool TenantName_Updated { get { return TenantName != SavedTenantName && TenantName != null; } }
        public bool Title_Updated { get { return Title.Value != SavedTitle && Title.Value != null; } }
        public bool Body_Updated { get { return Body != SavedBody && Body != null; } }
        public List<int> SwitchTargets;

        public TenantModel()
        {
        }

        public TenantModel(
            SiteSettings siteSettings, 
            Permissions.Types permissionType,
            bool setByForm = false,
            MethodTypes methodType = MethodTypes.NotSet)
        {
            OnConstructing();
            SiteSettings = siteSettings;
            PermissionType = permissionType;
            if (setByForm) SetByForm();
            MethodType = methodType;
            OnConstructed();
        }

        public TenantModel(
            SiteSettings siteSettings, 
            Permissions.Types permissionType,
            int tenantId,
            bool clearSessions = false,
            bool setByForm = false,
            List<int> switchTargets = null,
            MethodTypes methodType = MethodTypes.NotSet)
        {
            OnConstructing();
            SiteSettings = siteSettings;
            TenantId = tenantId;
            PermissionType = permissionType;
            Get();
            if (clearSessions) ClearSessions();
            if (setByForm) SetByForm();
            SwitchTargets = switchTargets;
            MethodType = methodType;
            OnConstructed();
        }

        public TenantModel(
            SiteSettings siteSettings, 
            Permissions.Types permissionType,
            DataRow dataRow)
        {
            OnConstructing();
            SiteSettings = siteSettings;
            Set(dataRow);
            OnConstructed();
        }

        private void OnConstructing()
        {
        }

        private void OnConstructed()
        {
        }

        public void ClearSessions()
        {
        }

        public TenantModel Get(
            Sqls.TableTypes tableType = Sqls.TableTypes.Normal,
            SqlColumnCollection column = null,
            SqlJoinCollection join = null,
            SqlWhereCollection where = null,
            SqlOrderByCollection orderBy = null,
            SqlParamCollection param = null,
            bool distinct = false,
            int top = 0)
        {
            Set(Rds.ExecuteTable(statements: Rds.SelectTenants(
                tableType: tableType,
                column: column ?? Rds.TenantsDefaultColumns(),
                join: join ??  Rds.TenantsJoinDefault(),
                where: where ?? Rds.TenantsWhereDefault(this),
                orderBy: orderBy ?? null,
                param: param ?? null,
                distinct: distinct,
                top: top)));
            return this;
        }

        public Error.Types Create(
            Sqls.TableTypes tableType = Sqls.TableTypes.Normal,
            SqlParamCollection param = null,
            bool paramAll = false)
        {
            var newId = Rds.ExecuteScalar_int(
                transactional: true,
                statements: new SqlStatement[]
                {
                    Rds.InsertTenants(
                        tableType: tableType,
                        selectIdentity: true,
                        param: param ?? Rds.TenantsParamDefault(
                            this, setDefault: true, paramAll: paramAll))
                });
            TenantId = newId != 0 ? newId : TenantId;
            Get();
            return Error.Types.None;
        }

        public Error.Types Update(SqlParamCollection param = null, bool paramAll = false)
        {
            SetBySession();
            var timestamp = Timestamp.ToDateTime();
            var count = Rds.ExecuteScalar_int(
                transactional: true,
                statements: new SqlStatement[]
                {
                    Rds.UpdateTenants(
                        verUp: VerUp,
                        where: Rds.TenantsWhereDefault(this)
                            .UpdatedTime(timestamp, _using: timestamp.InRange()),
                        param: param ?? Rds.TenantsParamDefault(this, paramAll: paramAll),
                        countRecord: true)
                });
            if (count == 0) return Error.Types.UpdateConflicts;
            Get();
            return Error.Types.None;
        }

        public string UpdateOrCreate(
            SqlWhereCollection where = null,
            SqlParamCollection param = null)
        {
            if (!PermissionType.CanEditSys())
            {
                return Messages.ResponseHasNotPermission().ToJson();
            }
            SetBySession();
            OnUpdatingOrCreating(ref where, ref param);
            var newId = Rds.ExecuteScalar_int(
                transactional: true,
                statements: new SqlStatement[]
                {
                    Rds.UpdateOrInsertTenants(
                        selectIdentity: true,
                        where: where ?? Rds.TenantsWhereDefault(this),
                        param: param ?? Rds.TenantsParamDefault(this, setDefault: true))
                });
            TenantId = newId != 0 ? newId : TenantId;
            Get();
            var responseCollection = new TenantsResponseCollection(this);
            OnUpdatedOrCreated(ref responseCollection);
            return responseCollection.ToJson();
        }

        private void OnUpdatingOrCreating(
            ref SqlWhereCollection where,
            ref SqlParamCollection param)
        {
        }

        private void OnUpdatedOrCreated(ref TenantsResponseCollection responseCollection)
        {
        }

        public string Delete(bool redirect = true)
        {
            if (!PermissionType.CanEditSys())
            {
                return Messages.ResponseHasNotPermission().ToJson();
            }
            OnDeleting();
            Rds.ExecuteNonQuery(
                transactional: true,
                statements: new SqlStatement[]
                {
                    Rds.DeleteTenants(
                        where: Rds.TenantsWhere().TenantId(TenantId))
                });
            Sessions.Set("Message", Messages.Deleted(Title.Value).Html);
            var responseCollection = new TenantsResponseCollection(this);
            OnDeleted(ref responseCollection);
            if (redirect)
            {
                responseCollection.Href(Navigations.Index("Tenants"));
            }
            return responseCollection.ToJson();
        }

        private void OnDeleting()
        {
        }

        private void OnDeleted(ref TenantsResponseCollection responseCollection)
        {
        }

        public string Restore(int tenantId)
        {
            if (!Permissions.Admins().CanEditTenant())
            {
                return Messages.ResponseHasNotPermission().ToJson();
            }
            TenantId = tenantId;
            Rds.ExecuteNonQuery(
                connectionString: Parameters.Rds.OwnerConnectionString,
                transactional: true,
                statements: new SqlStatement[]
                {
                    Rds.RestoreTenants(
                        where: Rds.TenantsWhere().TenantId(TenantId))
                });
            return new ResponseCollection().ToJson();
        }

        public string PhysicalDelete(Sqls.TableTypes tableType = Sqls.TableTypes.Normal)
        {
            if (!PermissionType.CanEditSys())
            {
                return Messages.ResponseHasNotPermission().ToJson();
            }
            OnPhysicalDeleting();
            Rds.ExecuteNonQuery(
                transactional: true,
                statements: Rds.PhysicalDeleteTenants(
                    tableType: tableType,
                    param: Rds.TenantsParam().TenantId(TenantId)));
            var responseCollection = new TenantsResponseCollection(this);
            OnPhysicalDeleted(ref responseCollection);
            return responseCollection.ToJson();
        }

        private void OnPhysicalDeleting()
        {
        }

        private void OnPhysicalDeleted(ref TenantsResponseCollection responseCollection)
        {
        }

        private void SetByForm()
        {
            Forms.Keys().ForEach(controlId =>
            {
                switch (controlId)
                {
                    case "Tenants_TenantName": TenantName = Forms.Data(controlId).ToString(); break;
                    case "Tenants_Title": Title = new Title(TenantId, Forms.Data(controlId)); break;
                    case "Tenants_Body": Body = Forms.Data(controlId).ToString(); break;
                    case "Tenants_Timestamp": Timestamp = Forms.Data(controlId).ToString(); break;
                    case "Comments": Comments = Comments.Prepend(Forms.Data("Comments")); break;
                    case "VerUp": VerUp = Forms.Data(controlId).ToBool(); break;
                    default: break;
                }
            });
            if (Routes.Action() == "deletecomment")
            {
                DeleteCommentId = Forms.ControlId().Split(',')._2nd().ToInt();
                Comments.RemoveAll(o => o.CommentId == DeleteCommentId);
            }
            Forms.FileKeys().ForEach(controlId =>
            {
                switch (controlId)
                {
                    default: break;
                }
            });
        }

        private void SetBySession()
        {
        }

        private void Set(DataTable dataTable)
        {
            switch (dataTable.Rows.Count)
            {
                case 1: Set(dataTable.Rows[0]); break;
                case 0: AccessStatus = Databases.AccessStatuses.NotFound; break;
                default: AccessStatus = Databases.AccessStatuses.Overlap; break;
            }
        }

        private void Set(DataRow dataRow)
        {
            AccessStatus = Databases.AccessStatuses.Selected;
            foreach(DataColumn dataColumn in dataRow.Table.Columns)
            {
                var name = dataColumn.ColumnName;
                switch(name)
                {
                    case "TenantId": if (dataRow[name] != DBNull.Value) { TenantId = dataRow[name].ToInt(); SavedTenantId = TenantId; } break;
                    case "Ver": Ver = dataRow[name].ToInt(); SavedVer = Ver; break;
                    case "TenantName": TenantName = dataRow[name].ToString(); SavedTenantName = TenantName; break;
                    case "Title": Title = new Title(dataRow, "TenantId"); SavedTitle = Title.Value; break;
                    case "Body": Body = dataRow[name].ToString(); SavedBody = Body; break;
                    case "Comments": Comments = dataRow["Comments"].ToString().Deserialize<Comments>() ?? new Comments(); SavedComments = Comments.ToJson(); break;
                    case "Creator": Creator = SiteInfo.User(dataRow.Int(name)); SavedCreator = Creator.Id; break;
                    case "Updator": Updator = SiteInfo.User(dataRow.Int(name)); SavedUpdator = Updator.Id; break;
                    case "CreatedTime": CreatedTime = new Time(dataRow, "CreatedTime"); SavedCreatedTime = CreatedTime.Value; break;
                    case "UpdatedTime": UpdatedTime = new Time(dataRow, "UpdatedTime"); Timestamp = dataRow.Field<DateTime>("UpdatedTime").ToString("yyyy/M/d H:m:s.fff"); SavedUpdatedTime = UpdatedTime.Value; break;
                    case "IsHistory": VerType = dataRow[name].ToBool() ? Versions.VerTypes.History : Versions.VerTypes.Latest; break;
                }
            }
        }

        private string Editor()
        {
            return new TenantsResponseCollection(this)
                .ReplaceAll(
                    "#MainContainer",
                    TenantUtilities.Editor(this))
                .Invoke("validateTenants")
                .ToJson();
        }
    }
}
