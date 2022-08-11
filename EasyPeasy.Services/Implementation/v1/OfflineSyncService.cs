using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.Services.Interface.v1;
using HahnLabs.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class OfflineSyncService : IOfflineSyncService
    {
        public async Task<Tuple<string, SyncContactRequest>> SyncContacts(SyncContactRequest model, string deviceToken, Guid userId)
        {
            try
            {
                SyncContactRequest response = new SyncContactRequest();

                using (var db = new EasypeasyDbContext())
                {

                    var addedEntities = model.contacts.Where(x => x.RequestTypeId == ESyncRequestType.Add).ToList();
                    var updatedEntities = model.contacts.Where(x => x.RequestTypeId == ESyncRequestType.Update).ToList();
                    var deletedEntities = model.contacts.Where(x => x.RequestTypeId == ESyncRequestType.Delete).ToList();

                    if (addedEntities.Any(x => x.Id != null))
                    {
                        return Tuple.Create("Added contact Ids must be null", response);
                    }

                    //var allPhones = addedEntities.Select(x => x.PhoneNumber).ToList();
                    //if (await db.UserContacts.AnyAsync(y => allPhones.Contains(y.PhoneNumber)))
                    //{
                    //    return Tuple.Create("Added contact already exists", response);
                    //}

                    if (updatedEntities.Any(x => x.Id == null))
                    {
                        return Tuple.Create("Updated contacts must have Ids", response);
                    }

                    if (deletedEntities.Any(x => x.Id == null))
                    {
                        return Tuple.Create("Deleted contacts must have Ids", response);
                    }

                    //Start Added Contacts
                    var addedContacts = new List<UserContacts>();
                    foreach (var x in addedEntities)
                    {
                        var findUser = await db.Users.FirstOrDefaultAsync(y => y.IsEnabled && y.PhoneNumber.Equals(x.PhoneNumber));
                        var contactId = SystemGlobal.GetId();
                        var userContact = new UserContacts
                        {
                            Id = contactId,
                            Company = x.Company,
                            CountryCode = x.CountryCode,
                            CreatedBy = userId.ToString(),
                            CreatedOn = DateTime.UtcNow,
                            CreatedOnDate = DateTime.UtcNow,
                            FirstName = x.FirstName,
                            IsEnabled = true,
                            IsInApp = findUser != null,
                            PhoneNumber = x.PhoneNumber,
                            LastName = x.LastName,
                            UserId = userId,
                            ContactUserId = findUser?.Id,
                            DeviceToken = deviceToken
                            //UserContactInformations = x.Email.Select(y => new UserContactInformations
                            //{
                            //    Id = SystemGlobal.GetId(),
                            //    ContactInformationTypeId = y.ContactInformationTypeId,
                            //    CreatedBy = userId.ToString(),
                            //    CreatedOn = DateTime.UtcNow,
                            //    CreatedOnDate = DateTime.UtcNow,
                            //    Date = y.Date,
                            //    IsEnabled = true,
                            //    Tag = y.Tag,
                            //    Text = y.Text,
                            //    UserContactId = contactId
                            //}).ToList()
                        };
                        userContact.UserContactInformations = BindContactInformations(contactId, userId, userContact, x);
                        addedContacts.Add(userContact);
                    }
                    await db.UserContacts.AddRangeAsync(addedContacts);
                    //End Added Contacts

                    //Start Updated Contacts
                    foreach (var x in updatedEntities)
                    {
                        var findContact = await db.UserContacts.Include(y => y.UserContactInformations).FirstOrDefaultAsync(y => y.IsEnabled && y.Id == x.Id);
                        if (findContact == null) continue;
                        {
                            var findUser = await db.Users.FirstOrDefaultAsync(y => y.IsEnabled && y.PhoneNumber.Equals(x.PhoneNumber));

                            findContact.Company = x.Company;
                            findContact.CountryCode = x.CountryCode;
                            findContact.UpdatedBy = userId.ToString();
                            findContact.UpdatedOn = DateTime.UtcNow;
                            findContact.FirstName = x.FirstName;
                            findContact.IsInApp = findUser != null;
                            findContact.PhoneNumber = x.PhoneNumber;
                            findContact.LastName = x.LastName;
                            findContact.UserId = userId;
                            findContact.ContactUserId = findUser?.Id;
                            db.UserContactInformations.RemoveRange(findContact.UserContactInformations.ToList());
                            findContact.UserContactInformations = BindContactInformations(findContact.Id, userId, findContact, x);
                            await db.SaveChangesAsync();

                        }
                    }
                    //End Updated Contacts

                    //Start Deleted Contacts
                    foreach (var x in deletedEntities)
                    {
                        var id = x.Id;
                        var findContact = await db.UserContacts.Include(y => y.UserContactInformations).FirstOrDefaultAsync(y => y.IsEnabled && y.Id == x.Id);

                        if (findContact != null)
                        {
                            findContact.IsEnabled = false;
                            findContact.DeletedBy = userId.ToString();
                            findContact.DeletedOn = DateTime.UtcNow;
                        }
                    }
                    //End Deleted Contacts

                    await db.SaveChangesAsync();
                    response.contacts = await db.UserContacts.Where(x => x.IsEnabled && x.UserId == userId).Select(x => new SyncUserContactRequest
                    {
                        Company = x.Company ?? "",
                        ContactUserId = db.Users.FirstOrDefault(y => y.IsEnabled && y.PhoneNumber.Equals(x.PhoneNumber)).Id,
                        CountryCode = x.CountryCode ?? "",
                        FirstName = x.FirstName ?? "",
                        IsInApp = db.Users.Any(y => y.IsEnabled && y.PhoneNumber.Equals(x.PhoneNumber)),
                        LastName = x.LastName ?? "",
                        PhoneNumber = x.PhoneNumber ?? "",
                        Id = x.Id,
                        Address = x.UserContactInformations.Where(y => y.IsEnabled && y.ContactInformationTypeId == (int)EContactInformationTypes.Address).Select(y => new SyncContactInformationRequest
                        {
                            Date = y.Date,
                            Tag = y.Tag ?? "",
                            Text = y.Text ?? ""
                        }).ToList(),
                        Date = x.UserContactInformations.Where(y => y.IsEnabled && y.ContactInformationTypeId == (int)EContactInformationTypes.Date).Select(y => new SyncContactInformationRequest
                        {
                            Date = y.Date,
                            Tag = y.Tag ?? "",
                            Text = y.Text ?? ""
                        }).ToList(),
                        Email = x.UserContactInformations.Where(y => y.IsEnabled && y.ContactInformationTypeId == (int)EContactInformationTypes.Email).Select(y => new SyncContactInformationRequest
                        {
                            Date = y.Date,
                            Tag = y.Tag ?? "",
                            Text = y.Text ?? ""
                        }).ToList(),
                        SocialLink = x.UserContactInformations.Where(y => y.IsEnabled && y.ContactInformationTypeId == (int)EContactInformationTypes.SocialLink).Select(y => new SyncContactInformationRequest
                        {
                            Date = y.Date,
                            Tag = y.Tag ?? "",
                            Text = y.Text ?? ""
                        }).ToList(),
                        Web = x.UserContactInformations.Where(y => y.IsEnabled && y.ContactInformationTypeId == (int)EContactInformationTypes.Web).Select(y => new SyncContactInformationRequest
                        {
                            Date = y.Date,
                            Tag = y.Tag ?? "",
                            Text = y.Text ?? ""
                        }).ToList()
                    }).ToListAsync();
                }
                return Tuple.Create("", response);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<UserContactInformations> BindContactInformations(Guid contactId, Guid userId, UserContacts userContact, SyncUserContactRequest x)
        {
            try
            {
                foreach (var y in x.Address)
                {
                    var userContactInformations = new UserContactInformations()
                    {
                        Id = SystemGlobal.GetId(),
                        ContactInformationTypeId = (int)EContactInformationTypes.Address,
                        CreatedBy = userId.ToString(),
                        CreatedOn = DateTime.UtcNow,
                        CreatedOnDate = DateTime.UtcNow,
                        Date = y.Date,
                        IsEnabled = true,
                        Tag = y.Tag,
                        Text = y.Text,
                        UserContactId = contactId
                    };
                    userContact.UserContactInformations.Add(userContactInformations);
                }
                foreach (var y in x.Date)
                {
                    var userContactInformations = new UserContactInformations()
                    {
                        Id = SystemGlobal.GetId(),
                        ContactInformationTypeId = (int)EContactInformationTypes.Date,
                        CreatedBy = userId.ToString(),
                        CreatedOn = DateTime.UtcNow,
                        CreatedOnDate = DateTime.UtcNow,
                        Date = y.Date,
                        IsEnabled = true,
                        Tag = y.Tag,
                        Text = y.Text,
                        UserContactId = contactId
                    };
                    userContact.UserContactInformations.Add(userContactInformations);
                }
                foreach (var y in x.Email)
                {
                    var userContactInformations = new UserContactInformations()
                    {
                        Id = SystemGlobal.GetId(),
                        ContactInformationTypeId = (int)EContactInformationTypes.Email,
                        CreatedBy = userId.ToString(),
                        CreatedOn = DateTime.UtcNow,
                        CreatedOnDate = DateTime.UtcNow,
                        Date = y.Date,
                        IsEnabled = true,
                        Tag = y.Tag,
                        Text = y.Text,
                        UserContactId = contactId
                    };
                    userContact.UserContactInformations.Add(userContactInformations);
                }
                foreach (var y in x.SocialLink)
                {
                    var userContactInformations = new UserContactInformations()
                    {
                        Id = SystemGlobal.GetId(),
                        ContactInformationTypeId = (int)EContactInformationTypes.SocialLink,
                        CreatedBy = userId.ToString(),
                        CreatedOn = DateTime.UtcNow,
                        CreatedOnDate = DateTime.UtcNow,
                        Date = y.Date,
                        IsEnabled = true,
                        Tag = y.Tag,
                        Text = y.Text,
                        UserContactId = contactId
                    };
                    userContact.UserContactInformations.Add(userContactInformations);
                }
                foreach (var y in x.Web)
                {
                    var userContactInformations = new UserContactInformations()
                    {
                        Id = SystemGlobal.GetId(),
                        ContactInformationTypeId = (int)EContactInformationTypes.Web,
                        CreatedBy = userId.ToString(),
                        CreatedOn = DateTime.UtcNow,
                        CreatedOnDate = DateTime.UtcNow,
                        Date = y.Date,
                        IsEnabled = true,
                        Tag = y.Tag,
                        Text = y.Text,
                        UserContactId = contactId
                    };
                    userContact.UserContactInformations.Add(userContactInformations);
                }
                return userContact.UserContactInformations.ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //public async Task<SyncContactRequest> SyncContacts(SyncContactRequest model, Guid userId)
        //{
        //    try
        //    {
        //        SyncContactRequest response = new SyncContactRequest();

        //        using (var db = new EasypeasyDbContext())
        //        {
        //            //switch (model.RequestTypeId)
        //            //{
        //            //    case ESyncRequestType.Add:

        //            //        var recievedContacts = new List<UserContacts>();

        //            //        foreach (var x in model.contacts)
        //            //        {
        //            //            var findUser = await db.Users.FirstOrDefaultAsync(y => y.IsEnabled && y.PhoneNumber.Equals(x.CountryCode + x.PhoneNumber));
        //            //            var contactId = SystemGlobal.GetId();
        //            //            recievedContacts.Add(new UserContacts
        //            //            {
        //            //                Id = x.Id ?? SystemGlobal.GetId(),
        //            //                Company = x.Company,
        //            //                CountryCode = x.CountryCode,
        //            //                CreatedBy = userId.ToString(),
        //            //                CreatedOn = DateTime.UtcNow,
        //            //                CreatedOnDate = DateTime.UtcNow,
        //            //                FirstName = x.FirstName,
        //            //                IsEnabled = true,
        //            //                IsInApp = findUser != null,
        //            //                PhoneNumber = x.PhoneNumber,
        //            //                LastName = x.LastName,
        //            //                UserId = userId,
        //            //                ContactUserId = findUser?.Id,
        //            //                //UserContactInformations = x.Email.Select(y => new UserContactInformations
        //            //                //{
        //            //                //    Id = SystemGlobal.GetId(),
        //            //                //    ContactInformationTypeId = y.ContactInformationTypeId,
        //            //                //    CreatedBy = userId.ToString(),
        //            //                //    CreatedOn = DateTime.UtcNow,
        //            //                //    CreatedOnDate = DateTime.UtcNow,
        //            //                //    Date = y.Date,
        //            //                //    IsEnabled = true,
        //            //                //    Tag = y.Tag,
        //            //                //    Text = y.Text,
        //            //                //    UserContactId = contactId
        //            //                //}).ToList()
        //            //            });

        //            //            await db.UserContacts.AddRangeAsync(recievedContacts);
        //            //        }
        //            //        model.contacts = MapToModel(recievedContacts);
        //            //        break;
        //            //    case ESyncRequestType.Update:
        //            //        var modelContactIds = model.contacts.Select(y => y.Id);
        //            //        var getMatchingContacts = await db.UserContacts.Where(x => x.IsEnabled && modelContactIds.Contains(x.Id)).ToListAsync();
        //            //        foreach (var x in getMatchingContacts)
        //            //        {
        //            //            var findUser = await db.Users.FirstOrDefaultAsync(y => y.IsEnabled && y.PhoneNumber.Equals(x.CountryCode + x.PhoneNumber));
        //            //            var requestItem = model.contacts.FirstOrDefault(x => x.Id == x.Id);
        //            //            x.Company = requestItem.Company;
        //            //            x.CountryCode = requestItem.CountryCode;
        //            //            x.UpdatedBy = userId.ToString();
        //            //            x.UpdatedOn = DateTime.UtcNow;
        //            //            x.FirstName = requestItem.FirstName;
        //            //            x.IsInApp = findUser != null;
        //            //            x.PhoneNumber = requestItem.PhoneNumber;
        //            //            x.LastName = requestItem.LastName;
        //            //            x.UserId = userId;
        //            //            x.ContactUserId = findUser?.Id;
        //            //            db.UserContactInformations.RemoveRange(x.UserContactInformations);
        //            //            //await db.SaveChangesAsync();
        //            //            //x.UserContactInformations = requestItem.informations.Select(y => new UserContactInformations
        //            //            //{
        //            //            //    Id = SystemGlobal.GetId(),
        //            //            //    ContactInformationTypeId = y.ContactInformationTypeId,
        //            //            //    CreatedBy = userId.ToString(),
        //            //            //    CreatedOn = DateTime.UtcNow,
        //            //            //    CreatedOnDate = DateTime.UtcNow,
        //            //            //    Date = y.Date,
        //            //            //    IsEnabled = true,
        //            //            //    Tag = y.Tag,
        //            //            //    Text = y.Text,
        //            //            //    UserContactId = x.Id
        //            //            //}).ToList();

        //            //        }

        //            //        model.contacts = MapToModel(getMatchingContacts);
        //            //        break;
        //            //    case ESyncRequestType.Delete:
        //            //        var modelContactIdsForDelete = model.contacts.Select(y => y.Id);
        //            //        var getMatchingContactsForDelete = await db.UserContacts.Where(x => x.IsEnabled && modelContactIdsForDelete.Contains(x.Id)).ToListAsync();
        //            //        getMatchingContactsForDelete
        //            //            .ForEach(x =>
        //            //            {
        //            //                x.IsEnabled = false;
        //            //                x.DeletedBy = userId.ToString();
        //            //                x.DeletedOn = DateTime.UtcNow;
        //            //            });
        //            //        break;
        //            //    default:
        //            //        break;
        //            //}

        //            //await db.SaveChangesAsync();
        //            response.contacts = model.contacts;
        //        }
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
    }
}
