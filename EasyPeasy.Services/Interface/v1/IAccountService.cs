
using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Requests;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using System;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IAccountService
    {
        Task<bool> Logout(string userId, string deviceToken, bool logoutFromAll);
        Task<AccountStaticDataResponse> GetStaticData(Guid userId);
        Task<bool> ChangePhoneNumberSendCode(PhoneNumberViewModel model, Guid userId);
        bool IsTokenValid(Guid userId, string deviceToken);
        bool IsAccountVerified(Guid userId);
        Task<Tuple<AccountLoginResponse, bool, bool>> Login(AccountLoginRequest model);
        Task<bool> ChangeForgotPassword(string phoneNumber, string newPassword);
        Task<bool> UpdatePassword(string oldPassword, string newPassword, Guid userId);
        Task<bool> ForgotSentVerificationCode(string phoneNumber);
        Task<bool> SignUp(AccountSignupRequest signUp);
        Task<bool> AddPhoneNumber(string countryCode, string phoneNumber, Guid userId);
        Task<bool> IsCodeVerified(string phoneNumber, string code, bool isChangingPhone, Guid userId);
        //Tuple<bool, LoginResponse> Login(LoginRequest login);
        //Task<bool> ForgotSentVerificationCode(string email);
        //Task<bool> ChangePassword(ChangePassword request);
        Task<bool> SubscriptionRequest(SubscribeRequests save);
    }
}
