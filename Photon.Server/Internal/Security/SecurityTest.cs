﻿using Photon.Library.Security;

namespace Photon.Server.Internal.Security
{
    internal static class SecurityTest
    {
        public static User AdminUser {get;}
        public static User GuestUser {get;}
        public static UserGroup AdminGroup {get;}
        public static UserGroup GuestGroup {get;}


        static SecurityTest()
        {
            AdminUser = new User {
                Id = "_admin_",
                Username = "admin",
                Password = "photon",
                DisplayName = "Administrator",
                IsEnabled = true,
            };

            GuestUser = new User {
                Id = "_guest_",
                Username = "guest",
                Password = "password",
                DisplayName = "Guest",
                IsEnabled = true,
            };

            AdminGroup = new UserGroup {
                UserIdList = {
                    AdminUser.Id,
                },
                RoleList = {
                    GroupRole.AgentView,
                    GroupRole.AgentEdit,
                    GroupRole.ProjectView,
                    GroupRole.ProjectEdit,
                    GroupRole.BuildView,
                    GroupRole.BuildStart,
                    GroupRole.BuildDelete,
                    GroupRole.DeployView,
                    GroupRole.DeployStart,
                    GroupRole.DeployDelete,
                    GroupRole.VariablesView,
                    GroupRole.VariablesEdit,
                    GroupRole.PackagesView,
                    GroupRole.PackagesDelete,
                    GroupRole.SecurityView,
                    GroupRole.SecurityEdit,
                    GroupRole.ConfigurationView,
                    GroupRole.ConfigurationEdit,
                }
            };

            GuestGroup = new UserGroup {
                UserIdList = {
                    GuestUser.Id,
                },
                RoleList = {
                    GroupRole.AgentView,
                    GroupRole.ProjectView,
                    GroupRole.BuildView,
                    GroupRole.DeployView,
                    GroupRole.VariablesView,
                    GroupRole.PackagesView,
                }
            };
        }

        public static void Initialize(UserGroupManager userMgr)
        {
            userMgr.Users.Add(AdminUser);
            userMgr.Users.Add(GuestUser);
            userMgr.Groups.Add(AdminGroup);
            userMgr.Groups.Add(GuestGroup);
        }
    }
}
