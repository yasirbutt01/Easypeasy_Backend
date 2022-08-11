using Audit.Core;
using Audit.WebApi;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Enum;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class AuditCustomDataService : AuditDataProvider
    {
        public AuditCustomDataService()
        {
        }
        public override object InsertEvent(AuditEvent auditEvent)
        {
            try
            {
                using (var _db = new EasypeasyDbContext())
                {
                    var settings = new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    };
                    Guid auditTrailId = (Guid)auditEvent.CustomFields[EAuditCustomFields.EventId];

                    if (auditEvent is AuditEventWebApi apiEvent)
                    {
                        var userId = auditEvent.CustomFields["UserId"]?.ToString() == ""
                            ? null
                            : auditEvent.CustomFields["UserId"]?.ToString();
                        var exludedColumns = new String[]
                        {
                        "IsEnabled", "CreatedOn", "CreatedBy", "UpdatedOn", "UpdatedBy", "DeletedOn", "DeletedBy",
                        "CreatedOnDate", "TenantIds"
                        };

                        var auditTrail = new AuditTrail()
                        {
                            Id = auditTrailId,
                            CreatedOn = DateTime.UtcNow,
                            IsEnabled = true,
                            CreatedOnDate = DateTime.UtcNow,
                            CreatedBy = userId,
                            UserName = auditEvent.Environment.UserName,
                            TraceId = auditEvent.CustomFields["TraceId"].ToString(),
                            EventStartOn = auditEvent.StartDate,
                            EventEndOn = auditEvent.EndDate,
                            EventDuration = auditEvent.Duration,
                            EventType = auditEvent.EventType,
                            Token = auditEvent.CustomFields["Token"].ToString()
                        };


                        auditTrail.ActionJsonData = JsonConvert.SerializeObject(apiEvent.Action, settings);
                        auditTrail.ActionName = apiEvent.Action.ActionName;
                        auditTrail.CallingMethod = apiEvent.Environment.CallingMethodName;
                        auditTrail.ControllerName = apiEvent.Action.ControllerName;
                        auditTrail.EnvironmentJsonData = JsonConvert.SerializeObject(apiEvent.Environment, settings);
                        auditTrail.IpAddress = apiEvent.Action.IpAddress;
                        auditTrail.HttpMethod = apiEvent.Action.HttpMethod;
                        auditTrail.RequestUrl = apiEvent.Action.RequestUrl;
                        auditTrail.ResponseCode = apiEvent.Action.ResponseStatusCode;
                        auditTrail.ResponseStatus = apiEvent.Action.ResponseStatus;
                        auditTrail.ExceptionJsonData = apiEvent.Action.Exception;


                        _db.AuditTrail.Add(auditTrail);
                        _db.SaveChanges();
                    }
                    return auditTrailId;
                }
            }
            catch (Exception ex)
            {
                return auditEvent;

            }

        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            try
            {
                using (var _db = new EasypeasyDbContext())
                {
                    var settings = new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    };
                    Guid auditTrailId = (Guid)auditEvent.CustomFields[EAuditCustomFields.EventId];

                    if (auditEvent is AuditEventWebApi apiEvent)
                    {
                        var userId = auditEvent.CustomFields["UserId"]?.ToString() == ""
                            ? null
                            : auditEvent.CustomFields["UserId"]?.ToString();
                        var exludedColumns = new String[]
                        {
                        "IsEnabled", "CreatedOn", "CreatedBy", "UpdatedOn", "UpdatedBy", "DeletedOn", "DeletedBy",
                        "CreatedOnDate", "TenantIds"
                        };

                        var auditTrail = new AuditTrail()
                        {
                            Id = auditTrailId,
                            CreatedOn = DateTime.UtcNow,
                            IsEnabled = true,
                            CreatedOnDate = DateTime.UtcNow,
                            CreatedBy = userId,
                            UserName = auditEvent.Environment.UserName,
                            TraceId = auditEvent.CustomFields["TraceId"].ToString(),
                            EventStartOn = auditEvent.StartDate,
                            EventEndOn = auditEvent.EndDate,
                            EventDuration = auditEvent.Duration,
                            EventType = auditEvent.EventType,
                            Token = auditEvent.CustomFields["Token"].ToString()
                        };


                        auditTrail.ActionJsonData = JsonConvert.SerializeObject(apiEvent.Action, settings);
                        auditTrail.ActionName = apiEvent.Action.ActionName;
                        auditTrail.CallingMethod = apiEvent.Environment.CallingMethodName;
                        auditTrail.ControllerName = apiEvent.Action.ControllerName;
                        auditTrail.EnvironmentJsonData = JsonConvert.SerializeObject(apiEvent.Environment, settings);
                        auditTrail.IpAddress = apiEvent.Action.IpAddress;
                        auditTrail.HttpMethod = apiEvent.Action.HttpMethod;
                        auditTrail.RequestUrl = apiEvent.Action.RequestUrl;
                        auditTrail.ResponseCode = apiEvent.Action.ResponseStatusCode;
                        auditTrail.ResponseStatus = apiEvent.Action.ResponseStatus;
                        auditTrail.ExceptionJsonData = apiEvent.Action.Exception;


                        await _db.AuditTrail.AddAsync(auditTrail);
                        await _db.SaveChangesAsync();
                    }
                    return auditTrailId;
                }
            }
            catch (Exception ex)
            {
                return auditEvent;

            }
        }

    }
}
