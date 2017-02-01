using Microsoft.AspNetCore.Identity;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Gravicode.AspNetCore.Identity.Redis
{
    public enum Keys
    {
        AspNetUsers, UserLoginInfo
    }

    public class UserStore<TUser> : IUserLoginStore<TUser>, IUserClaimStore<TUser>, 
        IUserRoleStore<TUser>, IUserPasswordStore<TUser>, IUserSecurityStampStore<TUser>
        where TUser : IdentityUser
    {
        public static class RedisKey
        {
            public static string Build(Keys key, object entity = null, string id = null)
            {
                var sb = new StringBuilder(AppNamespace);
                switch (key)
                {
                    case Keys.AspNetUsers:
                        {
                            sb.Append("aspnetusers:");
                            break;
                        }
                    case Keys.UserLoginInfo:
                        {
                            sb.Append("userlogins:");
                            var userLogin = (UserLoginInfo)entity;
                            sb.Append(userLogin.LoginProvider + ":");
                            sb.Append(userLogin.ProviderKey + ":");
                            break;
                        }
                    default: break;
                }
                if (id != null)
                    sb.Append(id.ToString());
                return sb.ToString().TrimEnd(':');
            }
        }

        private bool _disposed;

        private IRedisClient db;
        public static string AppNamespace;

        public UserStore(IRedisClient _db)
        {
            db = _db;
        }

        #region Internal
        private void ThrowIfDisposed()
        {
            //if (this._disposed)
            //    throw new ObjectDisposedException(this.GetType().Name);
        }
        public void Dispose()
        {
            this._disposed = true;
        }

        public void CheckDisposed(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
                throw new ArgumentNullException("user");
        }

        #endregion

        #region IUserLoginStore Implementation
        public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
           
            if (!user.Logins.Any(x => x.LoginProvider == login.LoginProvider && x.ProviderKey == login.ProviderKey))
            {
                user.Logins.Add(login);
            }

            return Task.FromResult(true);
        }

        public Task<TUser> FindAsync(UserLoginInfo login)
        {
            TUser user = null;
            var loginKeys = db.SearchKeys(RedisKey.Build(Keys.UserLoginInfo,entity: login));
            foreach (var loginKey in loginKeys)
            {
                var id = db.Get<string>(loginKey);
                if(id != string.Empty){
                    user = FindByIdAsync(id,CancellationToken.None).Result;
                    if (user != null)
                        break;
                }
            }
            return Task.FromResult(user);
        }
        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken)
     
        {
            CheckDisposed(user);

            return Task.FromResult((IList<UserLoginInfo>)user.Logins);
        }
        public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            CheckDisposed(user);

            user.Logins.RemoveAll(x => x.LoginProvider == loginProvider && x.ProviderKey == providerKey);

            return Task.FromResult(0);
        }
        public Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
    
        {
            CheckDisposed(user);
            user = CreateOrUpdate(user);
            return Task.FromResult(IdentityResult.Success);
        }

        public TUser CreateOrUpdate(TUser user, string userId=null)
        {
            var baseKey = RedisKey.Build(Keys.AspNetUsers);
            //insert
            if (string.IsNullOrEmpty(userId))
            {
                user.Id = db.IncrementValue(baseKey).ToString();
                var uniqueId = string.Format("{0}:{1}", user.UserName, user.Id);
                db.Add<TUser>(RedisKey.Build(Keys.AspNetUsers, id: uniqueId), user);
            }
            else
            {
                //update
                user.Id = userId;
                var NewUser = (TUser)Activator.CreateInstance(typeof(TUser));
                NewUser.Id = user.Id;
                NewUser.Email = user.Email;
                NewUser.NormalizedUserName = user.NormalizedUserName;
                NewUser.PasswordHash = user.PasswordHash;
                NewUser.SecurityStamp = user.SecurityStamp;
                NewUser.UserName = user.UserName;

                //logins
                
                for (int i = 0; i < user.Logins.Count; i++)
                {
                    UserLoginInfo info = new UserLoginInfo(user.Logins[i].LoginProvider, user.Logins[i].ProviderKey, user.Logins[i].ProviderDisplayName);
                    NewUser.Logins.Add(info);
                }
                //roles
                for (int i = 0; i < user.Roles.Count; i++)
                {
                    NewUser.Roles.Add((user.Roles[i]));
                }
                //claim
                for (int i = 0; i < user.Claims.Count; i++)
                {
                    NewUser.Claims.Add((user.Claims[i]));
                }

                var uniqueId = string.Format("{0}:{1}", NewUser.UserName, NewUser.Id);
                db.Set<TUser>(RedisKey.Build(Keys.AspNetUsers, id: uniqueId), NewUser);
            }
            
            //Add UserLogin Key-Value
            foreach (var login in user.Logins)
            {
                db.Set(RedisKey.Build(Keys.UserLoginInfo, entity: login), user.Id);
            }
            return user;
        }
        public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
 
        {
            CheckDisposed(user);
            var baseKey = RedisKey.Build(Keys.AspNetUsers);
            var uniqueKey = string.Format("{0}:{1}", user.UserName, user.Id);
            db.Remove(RedisKey.Build(Keys.AspNetUsers, id: uniqueKey));
            //Add UserLogin Key-Value
            foreach (var login in user.Logins)
            {
                var key = RedisKey.Build(Keys.UserLoginInfo, entity: login);
                if(db.ContainsKey(key))
                    db.Remove(key);
            }
            return Task.FromResult(IdentityResult.Success);
        }
        public Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
       
        {
            this.ThrowIfDisposed();
            var uniqueKey = string.Format("{0}:{1}", "*", userId);
            var keys = db.SearchKeys(RedisKey.Build(Keys.AspNetUsers, id: uniqueKey));
            if (keys.Any()) { uniqueKey = keys[0]; }
            var user = db.Get<TUser>(uniqueKey);
            return Task.FromResult(user);
        }
        public Task<TUser> FindByNameAsync(string userName, CancellationToken cancellationToken)
       
        {
            this.ThrowIfDisposed();
            userName = userName.ToLower();
            var uniqueKey = string.Format("{0}:{1}", userName, "*");
            var keys = db.SearchKeys(RedisKey.Build(Keys.AspNetUsers, id: uniqueKey));
            if (keys.Any()) { uniqueKey = keys[0]; }
            var user = db.Get<TUser>(uniqueKey);
            return Task.FromResult(user);
        }
        public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
   
        {
            CheckDisposed(user);
            var delres = DeleteAsync(user, CancellationToken.None);
            var createres = CreateOrUpdate(user, user.Id);
            return Task.FromResult(IdentityResult.Success);
        }

        #endregion

        #region IUserClaimStore Implementation

        public Task AddClaimAsync(TUser user, System.Security.Claims.Claim claim)
        {
            CheckDisposed(user);

            if (!user.Claims.Any(x => x.ClaimType == claim.Type && x.ClaimValue == claim.Value))
            {
                user.Claims.Add(new IdentityUserClaim
                {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }

            return Task.FromResult(0);
        }
        public Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken)
       
        {
            CheckDisposed(user);
            IList<Claim> result = user.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList();
            return Task.FromResult(result);
        }

        public Task RemoveClaimAsync(TUser user, System.Security.Claims.Claim claim)
        {
            CheckDisposed(user);
            user.Claims.RemoveAll(x => x.ClaimType == claim.Type && x.ClaimValue == claim.Value);
            return Task.FromResult(0);
        }

        #endregion

        #region IUserPasswordStore Implementation
        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
            return Task.FromResult(user.PasswordHash);
        }
        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
            return Task.FromResult<bool>(user.PasswordHash != null);
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
     
        {
            CheckDisposed(user);
            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        #endregion

        #region IUserSecurityStampStore Implementation
        public Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
     
        {
            CheckDisposed(user);
            return Task.FromResult(user.SecurityStamp);
        }
        public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
    
        {
            CheckDisposed(user);
            user.SecurityStamp = stamp;
            return Task.FromResult(0);
        }

        #endregion


        #region IUserRoleStore Implementation
        public Task AddToRoleAsync(TUser user, string role, CancellationToken cancellationToken)
      
        {
            CheckDisposed(user);
            if (!user.Roles.Contains(role, StringComparer.CurrentCultureIgnoreCase))
                user.Roles.Add(role);

            return Task.FromResult(true);
        }
        public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
    
        {
            CheckDisposed(user);
            return Task.FromResult<IList<string>>(user.Roles);
        }

        public Task<bool> IsInRoleAsync(TUser user, string role, CancellationToken cancellationToken)
    
        {
            CheckDisposed(user);
            return Task.FromResult(user.Roles.Contains(role, StringComparer.CurrentCultureIgnoreCase));

        }
        public Task RemoveFromRoleAsync(TUser user, string role, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
            user.Roles.RemoveAll(r => String.Equals(r, role, StringComparison.CurrentCultureIgnoreCase));

            return Task.FromResult(0);
        }
        //IUserLoginStore
        public Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            TUser user = null;
            var sb = new StringBuilder(AppNamespace);
            sb.Append("userlogins:");
            sb.Append(loginProvider + ":");
            sb.Append(providerKey + ":");
            var keystr =  sb.ToString().TrimEnd(':');
            var loginKeys = db.SearchKeys(keystr);
            foreach (var loginKey in loginKeys)
            {
                var id = db.Get<string>(loginKey);
                if (id != string.Empty)
                {
                    user = FindByIdAsync(id, CancellationToken.None).Result;
                    if (user != null)
                    {
                        if (user.Logins.Any(x => x.LoginProvider == loginProvider && x.ProviderKey == providerKey))
                        {
                            break;
                        }
                    }
                }
            }
            return Task.FromResult(user);
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            CheckDisposed(user);

            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            CheckDisposed(user);

            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        //IUserClaimStore

        public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
            foreach (var claim in claims)
            {
                if (!user.Claims.Any(x => x.ClaimType == claim.Type && x.ClaimValue == claim.Value))
                {
                    user.Claims.Add(new IdentityUserClaim
                    {
                        ClaimType = claim.Type,
                        ClaimValue = claim.Value
                    });
                }
                //UserClaims.Add(CreateUserClaim(user, claim));
            }
            return Task.FromResult(false);
        }

        public Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (newClaim == null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }

            var matchedClaims = user.Claims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToList();
            foreach (var matchedClaim in matchedClaims)
            {
                matchedClaim.ClaimValue = newClaim.Value;
                matchedClaim.ClaimType = newClaim.Type;
            }
            return Task.FromResult(false);
        }

        public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            CheckDisposed(user);
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
            foreach (var claim in claims)
            {
                var matchedClaims = user.Claims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToList();
                foreach (var c in matchedClaims)
                {
                    user.Claims.Remove(c);
                }
            }
            return Task.FromResult(false);
        }

        public Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            var streamClient = db.As<TUser>();
            var AllUsers = streamClient.GetAll();
            var query = from x in AllUsers
                        where x.Claims.Any(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type)
                        select x;
            IList<TUser> users = query.ToList();
            return Task.FromResult(users);
        }

        //IUserRoleStore

        public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                throw new ArgumentNullException(nameof(roleName));
            }
            var redisStream = db.As<IdentityRole>();
            var roleData = from c in redisStream.GetAll()
                           where c.Name == roleName
                           orderby c.Id
                           select c;
            var role = roleData.SingleOrDefault();
            var streamClient = db.As<TUser>();
            var AllUsers = streamClient.GetAll();

            if (role != null)
            {
                var query = from x in AllUsers
                            where x.Roles.Contains(role.Name, StringComparer.CurrentCultureIgnoreCase)
                            select x;
                IList<TUser> users = query.ToList();
                return Task.FromResult(users);
            }
            IList<TUser> xx = new List<TUser>();
            return Task.FromResult(xx);
        }
      
        #endregion
    }
}
