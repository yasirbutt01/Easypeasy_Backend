using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Requests.v2;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.DataViewModels.Response.v2;
using EasyPeasy.Services.Interface.v1;
using HahnLabs.Common;
using Microsoft.EntityFrameworkCore;
using IPaymentService = EasyPeasy.Services.Interface.v2.IPaymentService;
using PaymentAddPaymentMethodResponse = EasyPeasy.DataViewModels.Response.v2.PaymentAddPaymentMethodResponse;
using PaymentSubscribeNowRequest = EasyPeasy.DataViewModels.Requests.v2.PaymentSubscribeNowRequest;

namespace EasyPeasy.Services.Implementation.v2
{
    public class PaymentService : IPaymentService
    {
        private readonly IBrainTreeService brainTree;
        private INotificationService notificationService;
        private readonly IEmailTemplateService _emailTemplateService;

        public PaymentService(IBrainTreeService brainTree, INotificationService notificationService, IEmailTemplateService _emailTemplateService)
        {
            this.brainTree = brainTree;
            this.notificationService = notificationService;
            this._emailTemplateService = _emailTemplateService;
        }


        public async Task<DataViewModels.Response.v2.PaymentSubscriptionResponse> GetSubcriptions(Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var response = new DataViewModels.Response.v2.PaymentSubscriptionResponse();

                    response.CurrentPackage = await db.UserSubscriptions.Where(x => x.UserId == userId && x.PackageId != EPackage.Free1.ToId()).OrderByDescending(x => x.CreatedOn).Select(x => new DataViewModels.Response.v2.PaymentSubscriptionItemResponse
                    {
                        Id = x.Id,
                        CreationDate = x.CreatedOn,
                        PackageId = x.PackageId,
                        ExpiryDate = x.PsNextBillingDate,
                        Limit = x.Limit ?? -1,
                        Price = x.Price,
                        CouponPercentage = x.Coupon.DiscountPercentage,
                        CouponDiscount = x.UserInvoices.OrderByDescending(x => x.CreatedOn).FirstOrDefault().Discount ?? decimal.Zero,
                        InvoicePrice = x.UserInvoices.OrderByDescending(x => x.CreatedOn).FirstOrDefault().Price,
                        PackageName = x.Package.Name,
                        PackageColor = x.Package.ColorCode,
                        Status = x.PsStatus ?? "",
                        Code = x.UserInvoices.OrderByDescending(x => x.CreatedOn).FirstOrDefault().TransactionId,
                        PaymentMethod = new DataViewModels.Response.v2.PaymentAddPaymentMethodResponse
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

                    response.InvoiceHistory = await db.UserInvoices.Where(x => x.IsEnabled && x.UserSubscriptionId != null && x.UserId == userId).OrderByDescending(x => x.CreatedOn).Skip(1).Select(x => new DataViewModels.Response.v2.PaymentSubscriptionItemResponse
                    {
                        Id = x.UserSubscription.Id,
                        CreationDate = x.CreatedOn,
                        ExpiryDate = x.UserSubscription.PsNextBillingDate,
                        Limit = x.UserSubscription.Limit,
                        Price = x.UserSubscription.Price,
                        CouponDiscount = x.Discount ?? decimal.Zero,
                        CouponPercentage = x.Coupon.DiscountPercentage,
                        InvoicePrice = x.Price,
                        PackageName = x.UserSubscription.Package.Name,
                        PackageColor = x.UserSubscription.Package.ColorCode,
                        Status = x.UserSubscription.PsStatus ?? "",
                        Code = x.TransactionId,
                        PaymentMethod = new DataViewModels.Response.v2.PaymentAddPaymentMethodResponse
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

        public async Task<Tuple<PaymentVerifyCodeResponse, string>> VerifyCouponCode(PaymentVerifyCouponCodeRequest request, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                try
                {
                    var dateNow = DateTime.UtcNow;
                    var findCoupon = await db.Coupons.FirstOrDefaultAsync(x => x.IsEnabled &&
                                                                               x.Code.ToLower() == request.Code.ToLower());
                    if (findCoupon == null)
                    {
                        return Tuple.Create(new PaymentVerifyCodeResponse(), "Code did not match");

                    }

                    if (findCoupon.ExpiryDate < dateNow)
                    {
                        return Tuple.Create(new PaymentVerifyCodeResponse(), "Code has expired");

                    }

                    if (findCoupon.CouponTypeId != request.CouponTypeId)
                    {
                        return Tuple.Create(new PaymentVerifyCodeResponse(), "Code not valid for this product");

                    }

                    if ((findCoupon.PackageId != null &&
                         findCoupon.PackageId != request.PackageId))
                    {
                        return Tuple.Create(new PaymentVerifyCodeResponse(), "Code not valid for this package");

                    }

                    if (findCoupon.Limit <= db.UserSubscriptions.Count(x => x.CouponId == findCoupon.Id && x.UserId == userId))
                    {
                        return Tuple.Create(new PaymentVerifyCodeResponse(), "Code limit exceeded");

                    }

                    var response = new PaymentVerifyCodeResponse
                    {
                        CouponTypeId = findCoupon.CouponTypeId,
                        Code = findCoupon.Code,
                        Description = findCoupon.Description,
                        DiscountPercentage = findCoupon.DiscountPercentage,
                        Id = findCoupon.Id
                    };

                    return Tuple.Create(response, "");
                }
                catch (Exception ex)
                {
                    throw;
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

                        var dateNow = DateTime.UtcNow;
                        var findCoupon = await db.Coupons.FirstOrDefaultAsync(x => x.IsEnabled &&
                            x.Id == request.CouponId);

                        var discount = decimal.Zero;

                        if (getPaymentMethod.IsVerified != true)
                        {
                            return Tuple.Create(new AccountUserPackageViewModel(), "Please verify your payment method.");
                        }

                        if (findCoupon != null)
                        {
                            if (findCoupon.ExpiryDate < dateNow)
                            {
                                return Tuple.Create(new AccountUserPackageViewModel(), "Code has expired");
                            }

                            if ((findCoupon.PackageId != null &&
                                 findCoupon.PackageId != request.PackageId))
                            {
                                return Tuple.Create(new AccountUserPackageViewModel(), "Code not valid for this package");
                            }

                            if (findCoupon.Limit <= db.UserSubscriptions.Count(x => x.CouponId == findCoupon.Id && x.UserId == userId))
                            {
                                return Tuple.Create(new AccountUserPackageViewModel(), "Code limit exceeded");
                            }

                            discount = (findCoupon.DiscountPercentage / 100) * getPackage.Price;
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
                                UserPaymentMethodId = null,
                                CouponId = findCoupon?.Id
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
                                UserPaymentMethodId = request.PaymentMethodId,
                                CouponId = findCoupon?.Id
                            };

                            var invoice = new UserInvoices()
                            {
                                Id = Guid.NewGuid(),
                                InvoiceType = 1,
                                IsEnabled = true,
                                Price = getPackage.Price - discount,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                UserId = userId,
                                UserSubscriptionId = subscriptionId,
                                Discount = discount,
                                CouponId = findCoupon?.Id
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
                                    _ = _emailTemplateService.Subscription(getUser.UserProfiles?.FirstOrDefault()?.Email, getPackage.Name, invoice.Price, getPaymentMethod.CardMaskedNumber, getPaymentMethod.ExpiryDate, invoice.CreatedOn, invoice.TransactionId, getPaymentMethod.PaymentMethodImageUrl, getPaymentMethod.PaymentMethodTypeId);
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
                                UserPaymentMethodId = request.PaymentMethodId,
                                CouponId = findCoupon?.Id
                            };

                            var invoice = new UserInvoices()
                            {
                                Id = Guid.NewGuid(),
                                InvoiceType = 1,
                                IsEnabled = true,
                                Price = getPackage.Price - discount,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                UserId = userId,
                                UserSubscriptionId = subscriptionId,
                                Discount = discount,
                                CouponId = findCoupon?.Id
                            };
                            await db.UserSubscriptions.AddAsync(userSubscription);
                            await db.UserInvoices.AddAsync(invoice);
                            await db.SaveChangesAsync();

                            var braintreeSubscription = brainTree.CreateSubscription(getPackage.Id.ToString(), getPaymentMethod.Token, discount);
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
                                    _ = _emailTemplateService.Subscription(getUser.UserProfiles?.FirstOrDefault()?.Email,
                                        getPackage.Name, invoice.Price,
                                        getPaymentMethod.CardMaskedNumber,
                                        getPaymentMethod.ExpiryMonth + "/" + getPaymentMethod.ExpiryYear,
                                        invoice.CreatedOn, invoice.TransactionId,
                                        getPaymentMethod.PaymentMethodImageUrl,
                                        getPaymentMethod.PaymentMethodTypeId);
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
                                    Price = invoice.Price
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



    }
}
