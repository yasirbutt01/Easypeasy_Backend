using EasyPeasy.DataViewModels.Response.AdminResponse;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.Services.Interface.admin
{
    public interface IAdminAccountService
    {
        List<UserList> GetUsers(string SearchFilter, int skip, int take);
        UserList GetUser(string id);
        bool CheckUserStatus(string userName, string password);
        string FindUserByEmail(string email);
        string FindUserByUsername(string username);
        string FindUserIdByEnrolId(int enrolId);
        void UpdatePassword(string email, string password);
        CountryCodes GetPhoneNumberByDialCode(string dialCode);
        List<RoleList> getRoleList();
        List<string> GetRightByRoleId(string role);
        string GetRoleIdByUsername(string username);
        bool IsRightExist(string roleId, int rightId);
        int UserCountByRoleName(string roleName);
        List<CountryCodes> GetCountryCode();
        bool SaveRight(string roleId, int rightId);
        bool RemoveRight(string roleId, int rightId);
        bool CheckUserName(string UserName);
    }
}
