using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Response.v3;
using EasyPeasy.DataViewModels.Requests.v3;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;
using IPaymentService = EasyPeasy.Services.Interface.v3.IPaymentService;
using HahnLabs.Common;

namespace EasyPeasy.Services.Implementation.v3
{
    public class PaymentService: IPaymentService
    {
        private readonly IBrainTreeService brainTree;

        public PaymentService(IBrainTreeService brainTree)
        {
            this.brainTree = brainTree;
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
                        await RefundTransaction(getUserPaymentMethod, db);
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

        public async Task<Tuple<PaymentVerifyPaymentMethodResponse, string>> VerifyPaymentMethod(PaymentVerifyPaymentMethodRequest request, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                try
                {
                    var dateNow = DateTime.UtcNow;
                    var find = await db.UserPaymentMethods.FirstOrDefaultAsync(x => x.Id == request.PaymentMethodId);

                    if (find.AttemptCount < 1)
                    {
                        //find.AttemptCount = (find.AttemptCount ?? 0) + 1;
                        //await db.SaveChangesAsync();
                        return Tuple.Create(new PaymentVerifyPaymentMethodResponse() { IsVerified = find.IsVerified ?? false, AttemptCount = find.AttemptCount ?? 0 }, $"Attempt limit exceeded.");
                    }

                    if (request.VerificationAmount == find.VerificationAmount)
                    {

                        find.IsVerified = true;
                        await RefundTransaction(find, db);

                    }
                    else
                    {
                        find.AttemptCount = (find.AttemptCount ?? 3) - 1;
                        await db.SaveChangesAsync();

                        if (find.AttemptCount == 0)
                        {
                            await DeletePaymentMethod(find.Id, userId);
                        }

                        return Tuple.Create(new PaymentVerifyPaymentMethodResponse() { IsVerified = find.IsVerified ?? false, AttemptCount = find.AttemptCount ?? 0 }, $"Invalid amount entered.");
                    }



                    var response = new PaymentVerifyPaymentMethodResponse
                    {
                        AttemptCount = find.AttemptCount ?? 3,
                        IsVerified = find.IsVerified ?? false
                    };

                    return Tuple.Create(response, "");
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public async Task<bool> RefundTransaction(UserPaymentMethods find, EasypeasyDbContext db)
        {
            try
            {
                var transaction = await db.UserInvoices
                    .FirstOrDefaultAsync(x => x.PaymentMethodId == find.Id && x.InvoiceType == 3);

                if (transaction != null && (transaction.IsRefunded ?? false) == false)
                {

                    var findTranaction = brainTree.FindTransaction(transaction.TransactionId);

                    switch (findTranaction.Status)
                    {
                        case Braintree.TransactionStatus.SETTLING:
                        case Braintree.TransactionStatus.SETTLED:
                            var refund = brainTree.RefundTransaction(transaction.TransactionId);

                            if (refund.IsSuccess())
                            {
                                transaction.IsRefunded = true;
                                transaction.TransationStatus = refund.Target?.Status.ToString();
                                transaction.RefundedOn = DateTime.UtcNow;
                                transaction.RefundProcessorResponseCode =
                                    refund.Target?.ProcessorResponseCode;
                                transaction.RefundProcessorResponseText =
                                    refund.Target?.ProcessorResponseText;
                            }

                            break;
                        default:
                            var voidT = brainTree.VoidTransaction(transaction.TransactionId);


                            if (voidT.IsSuccess())
                            {
                                transaction.IsRefunded = true;
                                transaction.TransationStatus = voidT.Target?.Status.ToString();
                                transaction.RefundedOn = DateTime.UtcNow;
                                transaction.RefundProcessorResponseCode =
                                    voidT.Target?.ProcessorResponseCode;
                                transaction.RefundProcessorResponseText =
                                    voidT.Target?.ProcessorResponseText;
                            }

                            break;
                    }

                    await db.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception e)
            {
                throw;
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
                                paymentMethod.IsVerified = true;
                            }

                            if (x.Target is Braintree.CreditCard)
                            {

                                var random = new Random();
                                var verificationAmount = (decimal)random.NextDouble(1.00, 5.00);


                                var cardTarget = (Braintree.CreditCard)x.Target;
                                paymentMethod.IsVerified = false;
                                paymentMethod.VerificationAmount = verificationAmount;
                                paymentMethod.AttemptCount = 3;


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



                                var invoice = new UserInvoices()
                                {
                                    Id = Guid.NewGuid(),
                                    InvoiceType = 3,
                                    PaymentMethodId = paymentMethod.Id,
                                    IsEnabled = true,
                                    Price = verificationAmount,
                                    CreatedBy = userId.ToString(),
                                    CreatedOn = DateTime.UtcNow,
                                    CreatedOnDate = DateTime.UtcNow,
                                    UserId = userId,
                                    IsRefunded = false
                                };
                                await db.UserInvoices.AddAsync(invoice);

                                var braintreeTransaction = brainTree.CreateTransaction(userId.ToString(), verificationAmount, invoice.Id.ToString(), paymentMethod.Token, "Verification amount charged on " + DateTime.UtcNow.ToString("D"));
                                if (braintreeTransaction.IsSuccess())
                                {
                                    invoice.TransactionId = braintreeTransaction.Target.Id;
                                }
                                else
                                {
                                    brainTree.DeletePaymentMethod(userId.ToString(), paymentMethod.Token);
                                    await trans.RollbackAsync();
                                    return Tuple.Create(new PaymentAddPaymentMethodResponse(), "Could not charge card for verification amount.");
                                }




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
                                CreationDate = paymentMethod.CreatedOn,
                                AttemptCount = paymentMethod.AttemptCount ?? 0,
                                IsVerified = paymentMethod.IsVerified ?? false,
                                IsDefault = paymentMethod.IsDefault ?? false
                            };

                            return Tuple.Create(response, "");
                        }
                        else
                        {
                            // + "Card " + x.CreditCardVerification?.Status?.ToString()?.ToLower()?.Replace("_", " ")
                            var message = "";

                            if (!(string.IsNullOrEmpty(x.Message)))
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



    }
}
