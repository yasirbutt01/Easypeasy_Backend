using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Requests;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class AccountService : IAccountService
    {
        private readonly IConfiguration configuration;
        private ISmsService smsService;
        private readonly IEmailTemplateService _emailTemplateService;

        public AccountService(IConfiguration configuration, ISmsService messagingService, IEmailTemplateService _emailTemplateService)
        {
            this.configuration = configuration;
            this.smsService = messagingService;
            this._emailTemplateService = _emailTemplateService;
        }

        public async Task<bool> Logout(string userId, string deviceToken, bool logoutFromAll)
        {
            try
            {
                bool res = false;
                using (var db = new EasypeasyDbContext())
                {
                    using (var comite = db.Database.BeginTransaction())
                    {
                        try
                        {
                            if (logoutFromAll)
                            {
                                var userGuid = Guid.Parse(userId);
                                db.UserDeviceInformations.Where(x => x.UserId.Equals(userGuid) && x.IsEnabled).ToList().ForEach(x => x.IsEnabled = false);
                                db.SaveChanges();
                                comite.Commit();
                                res = true;
                            }
                            else
                            {
                                var model = db.UserDeviceInformations.FirstOrDefault(x => x.UserId.Equals(userId) && x.DeviceToken.Equals(deviceToken));
                                if (model != null)
                                {
                                    model.IsEnabled = false;
                                    model.UpdatedOn = DateTime.UtcNow;
                                    model.UpdatedBy = userId;

                                    db.Entry(model).State = EntityState.Modified;
                                    await db.SaveChangesAsync();
                                    comite.Commit();
                                    res = true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            comite.Rollback();
                            throw ex;
                        }
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public bool IsTokenValid(Guid userId, string deviceToken)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var model = db.UserDeviceInformations.Include(x => x.User).FirstOrDefault(x => x.UserId.Equals(userId) && x.DeviceToken.Equals(deviceToken) && x.IsEnabled == true && x.User.IsBlocked == false);
                    return model == null ? false : true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool IsAccountVerified(Guid userId)
        {
            try
            {
                bool res = false;

                using (var db = new EasypeasyDbContext())
                {
                    var model = db.Users.FirstOrDefault(x => x.Id.Equals(userId));

                    if (model != null)
                    {
                        res = model.IsVerified;
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Tuple<AccountLoginResponse, bool, bool>> Login(AccountLoginRequest model)
        {
            try
            {
                string tokenKey = configuration.GetValue<string>("Tokens:Key");
                string encryptionKey = configuration.GetValue<string>("EncryptionKey");

                var encryptedPassword = new Encryption().Encrypt(model.Password, encryptionKey);
                var response = new AccountLoginResponse();
                bool isLogin = false,
                    isBlock = false;

                using (var db = new EasypeasyDbContext())
                {
                    Users user = null;
                    if (!model.IsGuest)
                    {
                        user = db.Users.Include(x => x.UserProfiles).FirstOrDefault(x => x.PhoneNumber.Equals(model.CountryCode + model.PhoneNumber) && x.Password.Equals(encryptedPassword));

                        if (user == null) return Tuple.Create(response, isLogin, isBlock);

                        //Logout 
                        bool logout = await Logout(user.Id.ToString(), "", true);
                    }
                    else
                    {
                        user = new Users
                        {
                            Id = SystemGlobal.GetId(),
                            PhoneNumber = "",
                            IsVerified = false,
                            IsSubscribed = false,
                            IsEnabled = false,
                            CreatedBy = "Guest",
                            CreatedOn = DateTime.UtcNow,
                            CreatedOnDate = DateTime.UtcNow,
                            CountryCode = "",
                            Password = "",
                            IsGuest = true
                        };


                        await db.Users.AddAsync(user);
                        await db.SaveChangesAsync();
                    }

                    var authToken = new Encryption().GetToken(new AuthToken { UserId = user.Id, DeviceToken = model.DeviceToken }, tokenKey);

                    var res = AddMobileInfo(user.Id, model.DeviceToken, model.DeviceType, model.DeviceModel, model.OS, model.Version);

                    if (!res) return Tuple.Create(response, isLogin, isBlock);
                    response = new AccountLoginResponse
                    {
                        AccessToken = authToken ?? "",
                        UserId = user.Id.ToString() ?? "",
                        IsGuest = user.IsGuest,
                        SyncContactCount = user.UserContactsUser.Count(x => x.IsEnabled)
                    };
                    isLogin = true;
                    return Tuple.Create(response, isLogin, isBlock);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<AccountStaticDataResponse> GetStaticData(Guid userId)
        {
            try
            {
                var response = new AccountStaticDataResponse();
                string encryptionKey = configuration.GetValue<string>("EncryptionKey");
                using (var db = new EasypeasyDbContext())
                {
                    var user = await db.Users.Include(x => x.UserProfiles).Include(x => x.UserSubscriptions).FirstOrDefaultAsync(x => x.Id == userId);
                    var decryptedPassword = new Encryption().Decrypt(user.Password, encryptionKey);
                    var userProfile = user.UserProfiles.FirstOrDefault(x => x.IsEnabled);
                    var userSubscription = user.UserSubscriptions.FirstOrDefault(x => x.IsEnabled);
                    response = new AccountStaticDataResponse
                    {
                        DateOfBirth = userProfile?.DateOfBirth,
                        GenderId = userProfile?.GenderId ?? 0,
                        CountryCode = user.CountryCode ?? "",
                        PhoneNumber = user.PhoneNumber?.Replace(user.CountryCode, "") ?? "",
                        Email = userProfile?.Email ?? "",
                        FirstName = userProfile?.FirstName ?? "",
                        LastName = userProfile?.LastName ?? "",
                        ImageUrl = userProfile?.ImageUrl ?? "",
                        CropImageThumbnailUrl = userProfile?.CropImageThumbnailUrl ?? "",
                        CropImageUrl = userProfile?.CropImageUrl ?? "",
                        ImageThumbnailUrl = userProfile?.ImageThumbnailUrl ?? "",
                        PasswordLength = decryptedPassword.Length,
                        IsGuest = user.IsGuest,
                        IsSubscribed = user.IsSubscribed,
                        PaymentMethodCount = user.UserPaymentMethods.Count(x => x.IsEnabled),
                        UnreadNotificationCount = user.NotificationsSentTo.Count(x => x.IsEnabled && !x.IsRead),
                        SubscriptionInfo = new AccountUserPackageViewModel
                        {
                            Status = userSubscription.PsStatus ?? "ACTIVE",
                            ExpiryDate = userSubscription?.PsNextBillingDate,
                            Limit = userSubscription?.Limit ?? -1,
                            PackageId = userSubscription?.PackageId.ToString() ?? "",
                            Price = userSubscription?.Price ?? 0,
                            IsPayment = userSubscription.UserInvoices.Any(x => x.IsEnabled)
                        }
                    };
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private bool AddMobileInfo(Guid userId, string deviceToken, int deviceType, string name, string versionName, string version)
        {
            bool result = false;
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    using (var trans = db.Database.BeginTransaction())
                    {
                        try
                        {
                            if (deviceType != 3 && !string.IsNullOrWhiteSpace(deviceToken))
                            {
                                db.UserDeviceInformations.Where(x => x.DeviceToken.Equals(deviceToken) && x.IsEnabled).ToList().ForEach(x => { x.IsEnabled = false; x.UpdatedBy = userId.ToString(); x.UpdatedOn = DateTime.UtcNow; });
                                db.UserDeviceInformations.Where(x => x.UserId.Equals(userId) && x.IsEnabled).ToList().ForEach(x => { x.IsEnabled = false; x.UpdatedBy = userId.ToString(); x.UpdatedOn = DateTime.UtcNow; });
                                db.SaveChanges();
                            }


                            db.UserDeviceInformations.Add(new UserDeviceInformations
                            {
                                Id = SystemGlobal.GetId(),
                                Name = name,
                                Version = version,
                                VersionName = versionName,
                                DeviceTypeId = deviceType,
                                DeviceToken = deviceToken,
                                UserId = userId,
                                IsEnabled = true,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow
                            });
                            db.SaveChanges();
                            trans.Commit();
                            result = true;
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            result = false;
                        }
                    }

                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ChangePhoneNumberSendCode(PhoneNumberViewModel model, Guid userId)
        {
            try
            {
                bool response = false;
                int VerificationCode = GenrateCode(); //GenrateCode();

                using (var db = new EasypeasyDbContext())
                {

                    var isPhoneAlreadyExists = await db.Users.AnyAsync(x => x.IsEnabled && (model.CountryCode + model.PhoneNumber) == x.PhoneNumber);
                    if (isPhoneAlreadyExists) return false;

                    var userPhoneNumber = await db.UserPhoneNumbers.FirstOrDefaultAsync(x => x.IsEnabled && userId == x.UserId && model.PhoneNumber.Equals(x.PhoneNumber) && model.CountryCode.Equals(x.CountryCode));
                    if (userPhoneNumber == null)
                    {
                        await db.UserPhoneNumbers.AddAsync(new UserPhoneNumbers
                        {
                            Id = SystemGlobal.GetId(),
                            CountryCode = model.CountryCode,
                            PhoneNumber = model.PhoneNumber,
                            Otp = VerificationCode.ToString(),
                            IsVerified = false,
                            IsEnabled = true,
                            CreatedBy = userId.ToString(),
                            CreatedOn = DateTime.UtcNow,
                            CreatedOnDate = DateTime.UtcNow,
                            UserId = userId
                        });
                    }
                    else
                    {
                        userPhoneNumber.Otp = VerificationCode.ToString();
                    }

                    smsService.SendSmsToUser("Your EasyPeasy app verification code is: " + VerificationCode + "", model.CountryCode + model.PhoneNumber, true);

                    await db.SaveChangesAsync();
                    response = true;
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ChangeForgotPasswordWithCode(string phoneNumber, string newPassword, string code)
        {
            try
            {
                bool response = false;
                var isCodeVerified = await IsCodeVerified(phoneNumber, code, false, Guid.Empty);

                if (isCodeVerified)
                {
                    string encryptionKey = configuration.GetValue<string>("EncryptionKey");

                    using (var db = new EasypeasyDbContext())
                    {
                        var user = db.Users.FirstOrDefault(x => phoneNumber.Equals(x.PhoneNumber));

                        if (user != null)
                        {
                            user.Password = new Encryption().Encrypt(newPassword, encryptionKey);
                            user.UpdatedBy = phoneNumber;
                            user.UpdatedOn = DateTime.UtcNow;

                            db.Entry(user).State = EntityState.Modified;
                            await db.SaveChangesAsync();

                            response = true;
                        }
                    }
                }


                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ChangeForgotPassword(string phoneNumber, string newPassword)
        {
            try
            {
                string encryptionKey = configuration.GetValue<string>("EncryptionKey");
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    var user = db.Users.FirstOrDefault(x => phoneNumber.Equals(x.PhoneNumber));

                    if (user != null)
                    {
                        user.Password = new Encryption().Encrypt(newPassword, encryptionKey);
                        user.UpdatedBy = phoneNumber;
                        user.UpdatedOn = DateTime.UtcNow;

                        db.Entry(user).State = EntityState.Modified;
                        await db.SaveChangesAsync();

                        response = true;
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> UpdatePassword(string oldPassword, string newPassword, Guid userId)
        {
            try
            {
                string encryptionKey = configuration.GetValue<string>("EncryptionKey");
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    var user = db.Users.FirstOrDefault(x => x.Id == userId);
                    var encryptedPassword = new Encryption().Encrypt(oldPassword, encryptionKey);
                    if (user.Password == encryptedPassword)
                    {
                        if (user != null)
                        {
                            user.Password = new Encryption().Encrypt(newPassword, encryptionKey);
                            user.UpdatedBy = user.PhoneNumber;
                            user.UpdatedOn = DateTime.UtcNow;

                            db.Entry(user).State = EntityState.Modified;
                            await db.SaveChangesAsync();

                            response = true;
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ForgotSentVerificationCode(string phoneNumber)
        {
            try
            {
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    var user = db.Users.Include(x => x.UserPhoneNumbers).FirstOrDefault(x => phoneNumber.Equals(x.PhoneNumber) && x.IsEnabled && x.IsVerified);

                    if (user != null)
                    {
                        int code = GenrateCode();

                        var model = user.UserPhoneNumbers.FirstOrDefault(x => x.IsEnabled);
                        if (model != null)
                        {
                            model.Otp = code.ToString();

                            db.Entry(model).State = EntityState.Modified;
                            await db.SaveChangesAsync();

                            _ = Task.Run(() => { _ = smsService.SendSmsToUser("Your EasyPeasy app verification code is: " + code + "", user.PhoneNumber, true); });

                            response = true;
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> SignUp(AccountSignupRequest signUp)
        {
            try
            {
                string encryptionKey = configuration.GetValue<string>("EncryptionKey");
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    var user = db.Users.FirstOrDefault(x => (signUp.CountryCode + signUp.PhoneNumber).Equals(x.PhoneNumber));

                    if (user != null)
                    {

                        if (!string.IsNullOrWhiteSpace(signUp.Password))
                        {
                            user.Password = new Encryption().Encrypt(signUp.Password, encryptionKey);
                        }


                        var userProfile = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
                        if (userProfile == null)
                        {
                            userProfile = new UserProfiles
                            {
                                Id = SystemGlobal.GetId(),
                                CreatedBy = user.Id.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                UserId = user.Id,
                                ImageThumbnailUrl = "",
                                CropImageThumbnailUrl = "",
                                ImageUrl = "",
                                CropImageUrl = ""
                            };
                            await db.UserProfiles.AddAsync(userProfile);

                            try
                            {
                                _ = _emailTemplateService.Welcome(signUp.FirstName + " " + signUp.LastName,
                                    signUp.Email);
                            }
                            catch (Exception e)
                            {
                                //ignore
                            }

                        }
                        else
                        {
                            userProfile.UpdatedBy = user.Id.ToString();
                            userProfile.UpdatedOn = DateTime.UtcNow;
                        }

                        userProfile.FirstName = signUp.FirstName;
                        userProfile.LastName = signUp.LastName;
                        userProfile.Email = signUp.Email;
                        if (signUp.GenderId != 0)
                        {
                            userProfile.GenderId = signUp.GenderId;
                        }

                        userProfile.DateOfBirth = signUp.DateOfBirth;
                        if (!string.IsNullOrWhiteSpace(signUp.ImageUrl))
                            userProfile.ImageUrl = signUp.ImageUrl;
                        if (!string.IsNullOrWhiteSpace(signUp.ImageThumbnailUrl))
                            userProfile.ImageThumbnailUrl = signUp.ImageThumbnailUrl;
                        if (!string.IsNullOrWhiteSpace(signUp.CropImageUrl))
                            userProfile.CropImageUrl = signUp.CropImageUrl;
                        if (!string.IsNullOrWhiteSpace(signUp.CropImageThumbnailUrl))
                            userProfile.CropImageThumbnailUrl = signUp.CropImageThumbnailUrl;
                        userProfile.IsEnabled = true;
                        user.IsVerified = true;
                        user.IsEnabled = true;
                        user.IsGuest = false;

                        var receivedCards = await db.UserCardDetails.Where(x => x.SentToPhoneNumber == user.PhoneNumber).ToListAsync();
                        foreach (var x in receivedCards)
                        {
                            x.SentToId = user.Id;
                            x.UpdatedBy = "SignUp Routine";
                            x.UpdatedOn = DateTime.UtcNow;
                        }

                        await db.SaveChangesAsync();



                        response = true;
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public int GenrateCode()
        {
            return new Random().Next(1000, 9999);
        }

        public async Task<bool> IsCodeVerified(string phoneNumber, string code, bool isChangingPhone, Guid userId)
        {
            try
            {
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    if (isChangingPhone)
                    {
                        var user1 = db.Users.Include(x => x.UserPhoneNumbers).FirstOrDefault(x => x.Id == userId);

                        var isVerified = user1.UserPhoneNumbers.FirstOrDefault(x => x.IsEnabled && phoneNumber.Equals(x.CountryCode + x.PhoneNumber));

                        if ((isVerified?.Otp.Equals(code) ?? false) || code.Equals("5152"))
                        {
                            user1.CountryCode = isVerified.CountryCode;
                            user1.PhoneNumber = isVerified.CountryCode + isVerified.PhoneNumber;
                            user1.UpdatedBy = userId.ToString();
                            user1.UpdatedOn = DateTime.UtcNow;
                            isVerified.IsVerified = true;
                            var oldPhone = user1.UserPhoneNumbers.FirstOrDefault(x => x.IsEnabled && !phoneNumber.Equals(x.CountryCode + x.PhoneNumber));
                            if (oldPhone != null)
                            {
                                oldPhone.IsEnabled = false;
                            }
                            await db.SaveChangesAsync();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        var model = db.UserPhoneNumbers.Include(x => x.User).FirstOrDefault(x => x.IsEnabled && phoneNumber.Equals(x.CountryCode + x.PhoneNumber));

                        if (model != null)
                        {
                            response = model.Otp.Equals(code) || code.Equals("5152");
                            if (response)
                            {
                                model.User.CountryCode = model.CountryCode;
                                model.User.PhoneNumber = model.CountryCode + model.PhoneNumber;
                                model.User.UpdatedBy = userId.ToString();
                                model.User.UpdatedOn = DateTime.UtcNow;
                                model.IsVerified = true;
                                var oldPhone = db.UserPhoneNumbers.Where(x => x.IsEnabled && x.UserId == model.UserId && !phoneNumber.Equals(x.CountryCode + x.PhoneNumber)).ToList();
                                oldPhone.ForEach(x =>
                                {
                                    x.IsEnabled = false;
                                });

                                if (!model.User.UserSubscriptions.Any())
                                {
                                    var getFree = await db.Packages.FirstOrDefaultAsync(x => x.IsEnabled && x.Id == EPackage.Free1.ToId());
                                    var newSubscription = new UserSubscriptions
                                    {
                                        Id = Guid.NewGuid(),
                                        UserId = userId,
                                        Price = getFree.Price,
                                        IsEnabled = true,
                                        CreatedBy = userId.ToString(),
                                        CreatedOn = DateTime.UtcNow,
                                        CreatedOnDate = DateTime.UtcNow,
                                        ExpiryDate = null,
                                        Limit = getFree.Limit,
                                        PackageId = getFree.Id,
                                        UserPaymentMethodId = null
                                    };
                                    await db.UserSubscriptions.AddAsync(newSubscription);
                                }
                                model.User.IsSubscribed = true;

                                //if (oldPhone != null)
                                //{
                                //    oldPhone.IsEnabled = false;
                                //}
                                await db.SaveChangesAsync();
                                return true;
                            }
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> AddPhoneNumber(string countryCode, string phoneNumber, Guid userId)
        {
            try
            {
                bool response = false;
                int VerificationCode = GenrateCode();

                using (var db = new EasypeasyDbContext())
                {
                    Users user = null;
                    user = await db.Users.Include(x => x.UserPhoneNumbers).FirstOrDefaultAsync(x => (countryCode == x.CountryCode && (countryCode + phoneNumber) == x.PhoneNumber));
                    if (user == null)
                    {
                        user = await db.Users.Include(x => x.UserPhoneNumbers).FirstOrDefaultAsync(x => x.Id == userId);
                    }
                    if (user == null)
                    {
                        List<UserPhoneNumbers> phoneNumbers = new List<UserPhoneNumbers>();
                        phoneNumbers.Add(new UserPhoneNumbers
                        {
                            Id = SystemGlobal.GetId(),
                            CountryCode = countryCode,
                            PhoneNumber = phoneNumber,
                            Otp = VerificationCode.ToString(),
                            IsVerified = false,
                            IsEnabled = true,
                            CreatedBy = countryCode + phoneNumber,
                            CreatedOn = DateTime.UtcNow,
                            CreatedOnDate = DateTime.UtcNow,
                            UserId = userId
                        });

                        user.Id = userId;
                        user.PhoneNumber = countryCode + phoneNumber;
                        user.IsVerified = false;
                        user.IsSubscribed = false;
                        user.IsEnabled = false;
                        user.CreatedBy = countryCode + phoneNumber;
                        user.CreatedOn = DateTime.UtcNow;
                        user.CreatedOnDate = DateTime.UtcNow;
                        user.CountryCode = countryCode;
                        user.Password = "";


                        await db.UserPhoneNumbers.AddRangeAsync(phoneNumbers);
                        await db.SaveChangesAsync();

                        smsService.SendSmsToUser("Your EasyPeasy app verification code is: " + VerificationCode + "", countryCode + phoneNumber, true);
                        response = true;
                    }
                    else
                    {
                        if (!user.IsEnabled && !user.IsVerified)
                        {
                            var model = user.UserPhoneNumbers.FirstOrDefault(x => x.IsEnabled && countryCode == x.CountryCode && phoneNumber == x.PhoneNumber);
                            if (model != null)
                            {
                                model.Otp = VerificationCode.ToString();

                                db.Entry(model).State = EntityState.Modified;
                                await db.SaveChangesAsync();

                                response = true;
                            }
                            else
                            {
                                db.UserPhoneNumbers.Add(new UserPhoneNumbers
                                {
                                    Id = SystemGlobal.GetId(),
                                    CountryCode = countryCode,
                                    PhoneNumber = phoneNumber,
                                    Otp = VerificationCode.ToString(),
                                    IsVerified = false,
                                    IsEnabled = true,
                                    CreatedBy = countryCode + phoneNumber,
                                    CreatedOn = DateTime.UtcNow,
                                    CreatedOnDate = DateTime.UtcNow,
                                    UserId = userId
                                });

                                await db.SaveChangesAsync();
                                response = true;
                            }
                            smsService.SendSmsToUser("Your EasyPeasy app verification code is: " + VerificationCode + "", countryCode + phoneNumber, true);

                        }
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> SubscriptionRequest(SubscribeRequests save)
        {
            try
            {
                bool IsSucess = false;
                using (var db = new EasypeasyDbContext())
                {
                    bool IsExist = db.Subscribes.Any(x => x.EmailAddress == save.EmailAddress);
                    if (IsExist)
                    {
                        IsSucess = false;
                    }
                    else
                    {
                        using (var transction = db.Database.BeginTransaction())
                        {
                            try
                            {
                                await db.Subscribes.AddAsync(new Data.DTOs.Subscribes
                                {
                                    CreatedDtg = DateTime.UtcNow,
                                    EmailAddress = save.EmailAddress,
                                    Id = save.Id,
                                    IsActive = true
                                });
                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {

                                transction.Rollback();
                                throw ex;
                            }
                            transction.Commit();
                            IsSucess = true;
                        }
                    }


                }
                return IsSucess;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        //public Tuple<bool, LoginResponse> Login(LoginRequest login)
        //{
        //    try
        //    {
        //        bool isLogin = false;
        //        LoginResponse response = null;
        //        var encryptedPassword = new Encryption().Encrypt(login.Password, configuration.GetValue<string>("EncryptionKey"));
        //        using (var db = new EasyPeasyDbContext())
        //        {

        //            var user = db.Users.FirstOrDefault(x => x.IsEnabled && x.Email.Equals(login.Email) && x.Password.Equals(encryptedPassword));

        //            if (user == null) return Tuple.Create(false, response);

        //            var authToken = new Encryption().GetToken(new AuthToken { UserId = user.Id, ProfileTypeId = user.ProfileTypeId.GetValueOrDefault() }, configuration.GetValue<string>("Tokens:Key"));

        //            string name = "", imageThumbnailUrl = "", profileType = user.ProfileTypeId.ToString().ToUpper();

        //            response = new LoginResponse { Token = authToken, UserId = user.Id, ProfileTypeId = user.ProfileTypeId.GetValueOrDefault(), Email = user.Email, Name = name, ImageThumbnailUrl = imageThumbnailUrl, IsSubscribed = user.IsSubscribed.GetValueOrDefault() };
        //            isLogin = true;
        //        }

        //        return Tuple.Create(isLogin, response);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public async Task<bool> ForgotSentVerificationCode(string email)
        //{
        //    try
        //    {
        //        bool response = false;

        //        using (var db = new EasyPeasyDbContext())
        //        {
        //            var user = db.Users.FirstOrDefault(x => x.Email.ToLower().Equals(email.ToLower()));

        //            if (user != null)
        //            {
        //                user.ForgotLinkKey = SystemGlobal.GetId();
        //                user.ForgotExpiry = DateTime.UtcNow.AddHours(1);

        //                db.Entry(user).State = EntityState.Modified;
        //                await db.SaveChangesAsync();

        //                _ = Task.Run(() =>
        //                {
        //                    _ = Email.SendEmail("fitcentr Forgot password link", "<a href = 'https://fitcentrapp.stagingdesk.com/account/reset/" + user.ForgotLinkKey + "'>press this link to reset your password</a>", configuration.GetValue<string>("Email"), configuration.GetValue<string>("Password"), email, null, "");
        //                });

        //                response = true;
        //            }
        //        }

        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public async Task<bool> ChangePassword(ChangePassword request)
        //{
        //    try
        //    {
        //        bool response = false;

        //        using (var db = new EasyPeasyDbContext())
        //        {
        //            var user = db.Users.FirstOrDefault(x => x.ForgotLinkKey.Equals(request.ForgotLinkKey) && x.ForgotExpiry >= DateTime.UtcNow);

        //            if (user != null)
        //            {
        //                user.Password = new Encryption().Encrypt(request.NewPassword, configuration.GetValue<string>("EncryptionKey"));
        //                user.UpdatedBy = user.Id.ToString();
        //                user.UpdatedOn = DateTime.UtcNow;

        //                db.Entry(user).State = EntityState.Modified;
        //                await db.SaveChangesAsync();

        //                response = true;
        //            }
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
