using Braintree;
using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.Services.Interface.v1;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaypalService paypalService;
        private readonly IBrainTreeService brainTree;
        private INotificationService notificationService;
        private readonly IEmailTemplateService _emailTemplateService;

        public PaymentService(IPaypalService paypalService, IBrainTreeService brainTree, INotificationService notificationService, IEmailTemplateService _emailTemplateService)
        {
            this.paypalService = paypalService;
            this.brainTree = brainTree;
            this.notificationService = notificationService;
            this._emailTemplateService = _emailTemplateService;
        }

        public async Task<bool> BrainTreeWebHook(IFormCollection formCollection)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    if (formCollection != null)
                    {
                        var result = brainTree.WebHookNotificationParse(formCollection);
                        var braintreeSubscription = result?.Subscription;
                        if (braintreeSubscription != null)
                        {
                            var userSubscription = await db.UserSubscriptions.FirstOrDefaultAsync(x => x.PsSubscriptionId == result.Subscription.Id);
                            if (userSubscription != null)
                            {
                                userSubscription.PsBillingDayOfMonth = braintreeSubscription?.BillingDayOfMonth?.ToString();
                                userSubscription.PsBillingPeriodStartDate = braintreeSubscription?.BillingPeriodStartDate;
                                userSubscription.PsBillingPeriodEndDate = braintreeSubscription?.BillingPeriodEndDate;
                                userSubscription.PsFirstBillingDate = braintreeSubscription?.FirstBillingDate;
                                userSubscription.PsNextBillingDate = braintreeSubscription?.NextBillingDate;
                                userSubscription.PsPaidThroughDate = braintreeSubscription?.PaidThroughDate;
                                userSubscription.PsStatus = braintreeSubscription?.Status.ToString();
                                userSubscription.UpdatedBy = "BrainTree Webhook";
                                userSubscription.UpdatedOn = DateTime.UtcNow;
                                await db.SaveChangesAsync();

                                braintreeSubscription.Transactions?.ForEach(x =>
                                {
                                    if (!db.UserInvoices.Any(y => y.TransactionId == x.Id))
                                    {
                                        var invoice = new UserInvoices()
                                        {
                                            Id = Guid.NewGuid(),
                                            IsEnabled = true,
                                            Price = x.Amount ?? 0,
                                            CreatedBy = userSubscription.CreatedBy,
                                            CreatedOn = DateTime.UtcNow,
                                            CreatedOnDate = DateTime.UtcNow,
                                            UserId = userSubscription.UserId,
                                            UserSubscriptionId = userSubscription.Id,
                                            TransactionId = x.Id
                                        };
                                        db.UserInvoices.Add(invoice);
                                    }
                                });
                                await db.SaveChangesAsync();

                            }
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<bool> DeletePaymentMethod(Guid paymentMethodId, Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var getUserPaymentMethod = await db.UserPaymentMethods.FirstOrDefaultAsync(x => x.Id == paymentMethodId);
                    var braintreeResult = brainTree.DeletePaymentMethod(userId.ToString(), getUserPaymentMethod.Token);
                    if (braintreeResult)
                    {
                        getUserPaymentMethod.IsEnabled = false;
                        await db.SaveChangesAsync();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<bool> PaymentMethodMakeDefault(Guid paymentMethodId, Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var getUserPaymentMethods = await db.UserPaymentMethods.Where(x => x.IsEnabled && x.UserId == userId).ToListAsync();
                    var getUserPaymentMethod = getUserPaymentMethods.FirstOrDefault(x => x.Id == paymentMethodId);
                    var braintreeSubscription = brainTree.PaymentMethodMakeDefault(userId.ToString(), getUserPaymentMethod.Token);
                    getUserPaymentMethod.IsDefault = true;
                    getUserPaymentMethods.Where(x => x.IsEnabled && x.Id != paymentMethodId).ToList().ForEach(x =>
                    {
                        x.IsDefault = false;
                    });
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<bool> CancelSubscription(Guid subscriptionId, Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var userSubscription = await db.UserSubscriptions.FirstOrDefaultAsync(x => x.Id == subscriptionId);
                    if (userSubscription == null) return true;
                    if (userSubscription.PackageId == EPackage.EasyPeasyPremium.ToId())
                    {
                        return false;
                    }
                    var braintreeSubscription = brainTree.CancelSubscription(userSubscription.PsSubscriptionId);
                    userSubscription.PsBillingDayOfMonth = braintreeSubscription?.Target?.BillingDayOfMonth?.ToString();
                    userSubscription.PsBillingPeriodStartDate = braintreeSubscription?.Target?.BillingPeriodStartDate;
                    userSubscription.PsBillingPeriodEndDate = braintreeSubscription?.Target?.BillingPeriodEndDate;
                    userSubscription.PsFirstBillingDate = braintreeSubscription?.Target?.FirstBillingDate;
                    userSubscription.PsNextBillingDate = braintreeSubscription?.Target?.NextBillingDate;
                    userSubscription.PsPaidThroughDate = braintreeSubscription?.Target?.PaidThroughDate;
                    userSubscription.PsStatus = braintreeSubscription?.Target?.Status.ToString();
                    try
                    {
                        var getUser = await db.Users.Include(x => x.UserProfiles).FirstOrDefaultAsync(x => x.Id == userId);
                        var name = getUser.UserProfiles.FirstOrDefault()?.FirstName + " " +
                                   getUser.UserProfiles.FirstOrDefault()?.LastName;
                        var getPackage = await db.Packages.FirstOrDefaultAsync(x => x.IsEnabled && x.Id == userSubscription.PackageId);
                        _ = _emailTemplateService.UnSubscribed(getUser.UserProfiles?.FirstOrDefault()?.Email, getPackage.Name, userSubscription.PsNextBillingDate);
                    }
                    catch (Exception e)
                    {
                        //ignore
                    }
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<PaymentSubscriptionResponse> GetSubcriptions(Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var response = new PaymentSubscriptionResponse();

                    response.CurrentPackage = await db.UserSubscriptions.Where(x => x.UserId == userId && x.PackageId != EPackage.Free1.ToId()).OrderByDescending(x => x.CreatedOn).Select(x => new PaymentSubscriptionItemResponse
                    {
                        Id = x.Id,
                        CreationDate = x.CreatedOn,
                        PackageId = x.PackageId,
                        ExpiryDate = x.PsNextBillingDate,
                        Limit = x.Limit ?? -1,
                        Price = x.Price,
                        PackageName = x.Package.Name,
                        PackageColor = x.Package.ColorCode,
                        Status = x.PsStatus ?? "",
                        Code = x.PsSubscriptionId,
                        PaymentMethod = new PaymentAddPaymentMethodResponse
                        {
                            Id = x.UserPaymentMethod.Id,
                            CardMaskedCardNumber = x.UserPaymentMethod.CardMaskedNumber ?? "00000",
                            CardType = x.UserPaymentMethod.CardType,
                            ExpiryDate = x.UserPaymentMethod.ExpiryDate,
                            PaymentMethodType = x.UserPaymentMethod.PaymentMethodTypeId,
                            PaypalEmail = x.UserPaymentMethod.Email ?? "",
                            IsDefault = x.UserPaymentMethod.IsDefault ?? false,
                            PaymentMethodImageUrl = x.UserPaymentMethod.PaymentMethodImageUrl,
                            CreationDate = x.CreatedOn,
                        }
                    }).FirstOrDefaultAsync();

                    response.InvoiceHistory = await db.UserInvoices.Where(x => x.IsEnabled && x.UserSubscriptionId != null && x.UserId == userId).OrderByDescending(x => x.CreatedOn).Skip(1).Select(x => new PaymentSubscriptionItemResponse
                    {
                        Id = x.UserSubscription.Id,
                        CreationDate = x.CreatedOn,
                        ExpiryDate = x.UserSubscription.PsNextBillingDate,
                        Limit = x.UserSubscription.Limit,
                        Price = x.Price,
                        PackageName = x.UserSubscription.Package.Name,
                        PackageColor = x.UserSubscription.Package.ColorCode,
                        Status = x.UserSubscription.PsStatus ?? "",
                        Code = x.TransactionId,
                        PaymentMethod = new PaymentAddPaymentMethodResponse
                        {
                            Id = x.UserSubscription.UserPaymentMethod.Id,
                            CardMaskedCardNumber = x.UserSubscription.UserPaymentMethod.CardMaskedNumber,
                            CardType = x.UserSubscription.UserPaymentMethod.CardType,
                            ExpiryDate = x.UserSubscription.UserPaymentMethod.ExpiryDate,
                            PaymentMethodType = x.UserSubscription.UserPaymentMethod.PaymentMethodTypeId,
                            PaypalEmail = x.UserSubscription.UserPaymentMethod.Email ?? "",
                            IsDefault = x.UserSubscription.UserPaymentMethod.IsDefault ?? false,
                            PaymentMethodImageUrl = x.UserSubscription.UserPaymentMethod.PaymentMethodImageUrl,
                            CreationDate = x.CreatedOn,
                        }
                    }).ToListAsync();

                    return response;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<PaymentMethodResponse> GetPaymentMethods(Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var response = new PaymentMethodResponse();


                    var getBraintreeCustomer = brainTree.FindCustomerId(userId.ToString());
                    if (getBraintreeCustomer != null)
                    {
                        var getBraintreePaymentMethods = getBraintreeCustomer.PaymentMethods.ToList();
                        var getBrainTreePaymentMethodTokens = getBraintreePaymentMethods.Select(y => y.Token).ToList();
                        var getUserPaymentMethods = await db.UserPaymentMethods.Where(x => x.IsEnabled && x.UserId == userId).ToListAsync();
                        var getUserPaymentMethodTokens = getUserPaymentMethods.Select(y => y.Token).ToList();

                        var findDeletedPaymentMethods = getUserPaymentMethods.Where(x => x.IsEnabled && !getBrainTreePaymentMethodTokens.Contains(x.Token)).ToList();
                        findDeletedPaymentMethods.ForEach(x =>
                        {
                            x.IsEnabled = false;
                        });

                        getUserPaymentMethods.ForEach(x =>
                        {
                            x.IsDefault = getBraintreePaymentMethods.FirstOrDefault(y => y.Token == x.Token)?.IsDefault ?? false;
                        });

                        await db.SaveChangesAsync();

                        var getUserSubscription = await db.UserSubscriptions.FirstOrDefaultAsync(x => x.IsEnabled && x.UserId == userId);
                        //var x = getUserSubscription?.Package;
                        //if (x != null)
                        //{
                        //    response.SelectedPackage = new CommonGetPackagesResponse
                        //    {
                        //        Id = x.Id,
                        //        Description = x.Description,
                        //        Limit = x.Limit,
                        //        Name = x.Name,
                        //        Price = x.Price,
                        //        ColorCode = x.ColorCode,
                        //        Description2 = x.Description2,
                        //        DurationInDays = x.DurationInDays,
                        //        IsRecommended = x.IsRecommended,
                        //        packagePoints = x.PackagePoints.Select(y => new CommonGetPackagePointsResponse
                        //        {
                        //            Description = y.Description,
                        //            Status = y.Status
                        //        }).ToList()
                        //    };
                        //}
                        response.PaymentMethods = getUserPaymentMethods.Select(x => new PaymentAddPaymentMethodResponse
                        {
                            Id = x.Id,
                            CardMaskedCardNumber = x.CardMaskedNumber,
                            CardType = x.CardType,
                            ExpiryDate = x.ExpiryDate,
                            PaymentMethodType = x.PaymentMethodTypeId,
                            PaypalEmail = x.Email,
                            IsDefault = x.IsDefault ?? false,
                            PaymentMethodImageUrl = x.PaymentMethodImageUrl,
                            CreationDate = x.CreatedOn,
                            AttemptCount = x.AttemptCount ?? 3,
                            IsVerified = x.IsVerified ?? false
                        }).ToList();
                    }
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Tuple<AccountUserPackageViewModel, string>> SubscribeM(Guid packageId, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                using (var trans = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var getUser = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
                        var getPackage = await db.Packages.FirstOrDefaultAsync(x => x.Id == packageId && x.Price == decimal.Zero);

                        var subscriptionId = Guid.NewGuid();
                        var userSubscription = new UserSubscriptions
                        {
                            Id = subscriptionId,
                            UserId = userId,
                            Price = getPackage.Price,
                            IsEnabled = true,
                            CreatedBy = userId.ToString(),
                            CreatedOn = DateTime.UtcNow,
                            CreatedOnDate = DateTime.UtcNow,
                            ExpiryDate = DateTime.UtcNow.AddDays(getPackage.DurationInDays),
                            PsNextBillingDate = DateTime.UtcNow.AddDays(getPackage.DurationInDays),
                            Limit = getPackage.Limit,
                            PackageId = getPackage.Id,
                            UserPaymentMethodId = null
                        };
                        await db.UserSubscriptions.AddAsync(userSubscription);
                        var oldSubscriptions = getUser.UserSubscriptions.Where(x => x.IsEnabled && x.Id != subscriptionId).ToList();
                        oldSubscriptions.ForEach(y =>
                        {
                            y.IsEnabled = false;
                            y.DeletedBy = userId.ToString();
                            y.DeletedOn = DateTime.UtcNow;
                        });
                        getUser.IsSubscribed = true;
                        await db.SaveChangesAsync();
                        var res = await notificationService.PackageSubscribed(userId, userId, getPackage.Name, db);
                        await trans.CommitAsync();
                        return Tuple.Create(new AccountUserPackageViewModel
                        {
                            ExpiryDate = userSubscription.ExpiryDate,
                            Limit = userSubscription.Limit,
                            PackageId = userSubscription.PackageId.ToString(),
                            Price = userSubscription.Price
                        }, "");

                    }
                    catch (Exception ex)
                    {
                        await trans.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<Tuple<AccountUserPackageViewModel, string>> Subscribe(PaymentSubscribeNowRequest request, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                using (var trans = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var getUser = await db.Users.Include(x => x.UserProfiles).FirstOrDefaultAsync(x => x.Id == userId);
                        var getUserSubscription = getUser.UserSubscriptions.FirstOrDefault(x => x.IsEnabled);
                        var getPackage = await db.Packages.FirstOrDefaultAsync(x => x.IsEnabled && x.Id == request.PackageId);
                        var getPaymentMethod = await db.UserPaymentMethods.FirstOrDefaultAsync(x => x.IsEnabled && x.Id == request.PaymentMethodId);

                        if (getPaymentMethod.IsVerified != true)
                        {
                            return Tuple.Create(new AccountUserPackageViewModel(), "Please verify your payment method.");
                        }

                        var subscriptionId = Guid.NewGuid();
                        if (getPackage.Id == EPackage.Free1.ToId())
                        {
                            var getFreePackageOfUser = await db.UserSubscriptions.OrderByDescending(x => x.CreatedOn).FirstOrDefaultAsync(x => x.Package.Id == EPackage.Free1.ToId());
                            var getFree = await db.Packages.FirstOrDefaultAsync(x => x.IsEnabled && x.Id == EPackage.Free1.ToId());
                            var userSubscription = new UserSubscriptions
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId,
                                Price = getFree.Price,
                                IsEnabled = true,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                ExpiryDate = null,
                                Limit = getFreePackageOfUser.Limit,
                                PackageId = getFree.Id,
                                UserPaymentMethodId = null
                            };
                            await db.UserSubscriptions.AddAsync(userSubscription);
                            var oldSubscriptions = getUser.UserSubscriptions.Where(x => x.IsEnabled && x.Id != subscriptionId).ToList();
                            oldSubscriptions.ForEach(y =>
                            {
                                y.IsEnabled = false;
                                y.DeletedBy = userId.ToString();
                                y.DeletedOn = DateTime.UtcNow;
                            });
                            await db.SaveChangesAsync();
                            await trans.CommitAsync();
                            return Tuple.Create(new AccountUserPackageViewModel
                            {
                                ExpiryDate = userSubscription.ExpiryDate,
                                Limit = userSubscription.Limit,
                                PackageId = userSubscription.PackageId.ToString(),
                                Price = userSubscription.Price
                            }, "");
                        }

                        if (getPackage.Id == EPackage.Freelancer.ToId())
                        {
                            var userSubscription = new UserSubscriptions()
                            {
                                Id = subscriptionId,
                                UserId = userId,
                                Price = getPackage.Price,
                                IsEnabled = true,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                ExpiryDate = null,
                                Limit = getPackage.Limit,
                                PackageId = getPackage.Id,
                                UserPaymentMethodId = request.PaymentMethodId
                            };

                            var invoice = new UserInvoices()
                            {
                                Id = Guid.NewGuid(),
                                InvoiceType = 1,
                                IsEnabled = true,
                                Price = getPackage.Price,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                UserId = userId,
                                UserSubscriptionId = subscriptionId
                            };
                            await db.UserSubscriptions.AddAsync(userSubscription);
                            await db.UserInvoices.AddAsync(invoice);
                            await db.SaveChangesAsync();

                            var name = getUser.UserProfiles.FirstOrDefault()?.FirstName + "" +
                                       getUser.UserProfiles.FirstOrDefault()?.LastName;
                            var braintreeTransaction = brainTree.CreateTransaction(userId.ToString(), getPackage.Price, invoice.Id.ToString(), getPaymentMethod.Token, name + " purchased " + EPackage.Freelancer.ToString() + " package on " + DateTime.UtcNow.ToString("D"));
                            if (braintreeTransaction.IsSuccess())
                            {
                                invoice.TransactionId = braintreeTransaction.Target.Id;
                                userSubscription.PsFirstTransactionId = braintreeTransaction.Target.Id;

                                var oldSubscriptions = getUser.UserSubscriptions.Where(x => x.IsEnabled && x.Id != subscriptionId).ToList();
                                oldSubscriptions.ForEach(y =>
                                {
                                    y.IsEnabled = false;
                                    y.DeletedBy = userId.ToString();
                                    y.DeletedOn = DateTime.UtcNow;
                                });
                                getUser.IsSubscribed = true;
                                await db.SaveChangesAsync();
                                var res = await notificationService.PackageSubscribed(userId, userId, getPackage.Name, db);
                                await trans.CommitAsync();

                                try
                                {
                                    _ = _emailTemplateService.Subscription(getUser.UserProfiles?.FirstOrDefault()?.Email, getPackage.Name, getPackage.Price, getPaymentMethod.CardMaskedNumber, getPaymentMethod.ExpiryDate, invoice.CreatedOn, invoice.TransactionId, getPaymentMethod.PaymentMethodImageUrl, getPaymentMethod.PaymentMethodTypeId);
                                }
                                catch (Exception e)
                                {
                                    //ignore
                                }

                                return Tuple.Create(new AccountUserPackageViewModel
                                {
                                    ExpiryDate = userSubscription.ExpiryDate,
                                    Limit = userSubscription.Limit,
                                    PackageId = userSubscription.PackageId.ToString(),
                                    Price = userSubscription.Price
                                }, "");
                            }

                            await trans.RollbackAsync();
                            return Tuple.Create(new AccountUserPackageViewModel(), "Error: " + braintreeTransaction.Message);
                            // Handle errors
                        }

                        if (getPackage.Id == EPackage.Correspondent.ToId() || getPackage.Id == EPackage.Screenwriter.ToId() || getPackage.Id == EPackage.InkSlinger.ToId())
                        {
                            var userSubscription = new UserSubscriptions()
                            {
                                Id = subscriptionId,
                                UserId = userId,
                                Price = getPackage.Price,
                                IsEnabled = true,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                ExpiryDate = DateTime.UtcNow.AddDays(getPackage.DurationInDays),
                                Limit = getPackage.Limit,
                                PackageId = getPackage.Id,
                                UserPaymentMethodId = request.PaymentMethodId
                            };

                            var invoice = new UserInvoices()
                            {
                                Id = Guid.NewGuid(),
                                InvoiceType = 1,
                                IsEnabled = true,
                                Price = getPackage.Price,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                UserId = userId,
                                UserSubscriptionId = subscriptionId
                            };
                            await db.UserSubscriptions.AddAsync(userSubscription);
                            await db.UserInvoices.AddAsync(invoice);
                            await db.SaveChangesAsync();

                            var braintreeSubscription = brainTree.CreateSubscription(getPackage.Id.ToString(), getPaymentMethod.Token, 0);
                            if (braintreeSubscription.IsSuccess())
                            {
                                userSubscription.PsFirstTransactionId = braintreeSubscription.Target?.Transactions?.FirstOrDefault()?.Id;
                                userSubscription.PsSubscriptionId = braintreeSubscription.Target?.Id;
                                userSubscription.PsBillingDayOfMonth = braintreeSubscription.Target?.BillingDayOfMonth?.ToString();
                                userSubscription.PsBillingPeriodStartDate = braintreeSubscription.Target?.BillingPeriodStartDate;
                                userSubscription.PsBillingPeriodEndDate = braintreeSubscription.Target?.BillingPeriodEndDate;
                                userSubscription.PsFirstBillingDate = braintreeSubscription.Target?.FirstBillingDate;
                                userSubscription.PsNextBillingDate = braintreeSubscription.Target?.NextBillingDate;
                                userSubscription.PsPaidThroughDate = braintreeSubscription.Target?.PaidThroughDate;
                                userSubscription.PsStatus = braintreeSubscription.Target?.Status.ToString();

                                invoice.TransactionId = braintreeSubscription.Target?.Transactions?.FirstOrDefault()?.Id;
                                var oldSubscriptions = getUser.UserSubscriptions.Where(x => x.IsEnabled && x.Id != subscriptionId).ToList();
                                oldSubscriptions.ForEach(y =>
                                {
                                    y.IsEnabled = false;
                                    y.DeletedBy = userId.ToString();
                                    y.DeletedOn = DateTime.UtcNow;
                                });
                                getUser.IsSubscribed = true;
                                await db.SaveChangesAsync();
                                var res = await notificationService.PackageSubscribed(userId, userId, getPackage.Name, db);
                                try
                                {
                                    _ = _emailTemplateService.Subscription(getUser.UserProfiles?.FirstOrDefault()?.Email, getPackage.Name, getPackage.Price, getPaymentMethod.CardMaskedNumber, getPaymentMethod.ExpiryMonth + "/" + getPaymentMethod.ExpiryYear, invoice.CreatedOn, invoice.TransactionId, getPaymentMethod.PaymentMethodImageUrl, getPaymentMethod.PaymentMethodTypeId);
                                }
                                catch (Exception e)
                                {
                                    //ignore
                                }
                                await trans.CommitAsync();
                                return Tuple.Create(new AccountUserPackageViewModel
                                {
                                    ExpiryDate = userSubscription.ExpiryDate,
                                    Limit = userSubscription.Limit ?? -1,
                                    PackageId = userSubscription.PackageId.ToString(),
                                    Price = userSubscription.Price
                                }, "");
                            }

                            await trans.RollbackAsync();
                            return Tuple.Create(new AccountUserPackageViewModel(), "Error: " + braintreeSubscription.Message);
                            // Handle errors
                        }
                        return Tuple.Create(new AccountUserPackageViewModel(), "Invalid Request");
                    }
                    catch (Exception ex)
                    {
                        await trans.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<Tuple<PaymentAddPaymentMethodResponse, string>> AddPaymentMethod(PaymentAddPaymentMethodRequest request, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                using (var trans = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var x = await brainTree.CreatePaymentMethod(request.NOnce, userId);


                        //if (!string.IsNullOrWhiteSpace(x.Message))
                        //{
                        //    return Tuple.Create(new PaymentAddPaymentMethodResponse(), x.Message);
                        //}

                        if (x.IsSuccess())
                        {
                            if (db.UserPaymentMethods.Any(y => y.IsEnabled && y.UserId == userId && y.Token == x.Target.Token))
                            {
                                return Tuple.Create(new PaymentAddPaymentMethodResponse(), "Payment method already exists");
                            }

                            var paymentMethod = new UserPaymentMethods
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId,
                                IsEnabled = true,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow
                            };


                            if (x.Target is Braintree.PayPalAccount)
                            {
                                var paypalTarget = (Braintree.PayPalAccount)x.Target;
                                paymentMethod.PaymentMethodTypeId = (int)EPaymentMethodType.Paypal;
                                paymentMethod.Token = paypalTarget.Token;
                                paymentMethod.PaypalBillingAgreementId = paypalTarget.BillingAgreementId;
                                paymentMethod.Email = paypalTarget.Email;
                                paymentMethod.PayerId = paypalTarget.PayerId;
                                paymentMethod.IsDefault = paypalTarget.IsDefault;
                                paymentMethod.PaymentMethodImageUrl = paypalTarget.ImageUrl;

                            }

                            if (x.Target is Braintree.CreditCard)
                            {
                                var cardTarget = (Braintree.CreditCard)x.Target;
                                paymentMethod.PaymentMethodTypeId = (int)EPaymentMethodType.Card;
                                paymentMethod.Token = cardTarget.Token;
                                paymentMethod.ExpiryDate = cardTarget.ExpirationDate;
                                paymentMethod.CardMaskedNumber = cardTarget.MaskedNumber;
                                paymentMethod.CardLastDigits = cardTarget.LastFour;
                                paymentMethod.CardFirstDigits = cardTarget.Bin;
                                paymentMethod.VerifiedAmount = cardTarget.Verification?.Amount;
                                paymentMethod.VerifiedStatus = cardTarget.Verification?.Status?.ToString();
                                paymentMethod.CardHolderName = cardTarget.CardholderName;
                                paymentMethod.CardType = cardTarget.CardType.ToString();
                                paymentMethod.ExpiryYear = cardTarget.ExpirationYear;
                                paymentMethod.ExpiryMonth = cardTarget.ExpirationMonth;
                                paymentMethod.IsDefault = cardTarget.IsDefault;
                                paymentMethod.CardCurrency = cardTarget.Verification?.CurrencyIsoCode;
                                paymentMethod.PaymentMethodImageUrl = cardTarget.ImageUrl;

                            }

                            await db.UserPaymentMethods.AddAsync(paymentMethod);
                            await db.SaveChangesAsync();
                            await trans.CommitAsync();

                            var response = new PaymentAddPaymentMethodResponse()
                            {
                                Id = paymentMethod.Id,
                                CardMaskedCardNumber = paymentMethod.CardMaskedNumber,
                                CardType = paymentMethod.CardType,
                                ExpiryDate = paymentMethod.ExpiryDate,
                                PaymentMethodType = paymentMethod.PaymentMethodTypeId,
                                PaypalEmail = paymentMethod.Email,
                                PaymentMethodImageUrl = paymentMethod.PaymentMethodImageUrl,
                                CreationDate = paymentMethod.CreatedOn
                            };

                            return Tuple.Create(response, "");
                        }
                        else
                        {
                            // + "Card " + x.CreditCardVerification?.Status?.ToString()?.ToLower()?.Replace("_", " ")
                            var message = "";

                            if(!(string.IsNullOrEmpty(x.Message)))
                            {
                                message = "Card " + (x.Message.Contains(':') ? x.Message.Split(':')?.FirstOrDefault() : x.Message) + "";
                            }
                            return Tuple.Create(new PaymentAddPaymentMethodResponse(), message);

                        }

                    }
                    catch (Exception ex)
                    {
                        await trans.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<string> GenerateBrainTreeTokenAsync()
        {
            var clientToken = await brainTree.GenerateBrainTreeTokenAsync();
            return clientToken;
        }

        public async Task<string> SubscribePaypal(Guid packageId, Guid userId)
        {
            //using (var db = new EasypeasyDbContext())
            //{
            //    using (var trans = await db.Database.BeginTransactionAsync())
            //    {
            //        try
            //        {
            //            var getUser = await db.Users.Include(x => x.UserProfiles).FirstOrDefaultAsync(x => x.Id == userId);
            //            var getUserProfile = getUser.UserProfiles.FirstOrDefault(x => x.IsEnabled);
            //            var getPackage = await db.Packages.FirstOrDefaultAsync(x => x.Id == packageId);
            //            var paypalSubscriptionCreationTime = DateTime.UtcNow.AddMinutes(3);
            //            var subscriptionResponse = await paypalService.CreatePaypalSubscriptionAsync(getPackage.PaypalPlanId, getUserProfile.FirstName, getUserProfile.LastName, getUserProfile.Email, paypalSubscriptionCreationTime);
            //            await db.UserSubscriptions.AddAsync(new Data.DTOs.UserSubscriptions
            //            {
            //                Id = SystemGlobal.GetId(),
            //                IsEnabled = false,
            //                CreatedBy = userId.ToString(),
            //                CreatedOn = DateTime.UtcNow,
            //                CreatedOnDate = DateTime.UtcNow,
            //                Limit = getPackage.Limit,
            //                ExpiryDate = DateTime.UtcNow.AddDays(getPackage.DurationInDays),
            //                PackageId = packageId,
            //                PaypalSubscriptionCreationTime = subscriptionResponse.create_time,
            //                Price = getPackage.Price,
            //                UserId = userId,
            //                PaypalSubscriptionId = subscriptionResponse.id
            //            });
            //            await db.SaveChangesAsync();
            //            await trans.CommitAsync();
            //            return subscriptionResponse.links.FirstOrDefault(x => x.rel == "approve").href;
            //        }
            //        catch (Exception ex)
            //        {
            //            await trans.RollbackAsync();
            //            throw;
            //        }
            //    }
            //}
            return "";
        }
    }
}
