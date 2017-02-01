// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Internal;
using Gravicode.AspNetCore.Identity.Redis;
using ServiceStack.Redis;

namespace Gravicode.AspNetCore.Identity.Redis
{
    /// <summary>
    /// Creates a new instance of a persistence store for roles.
    /// </summary>
    /// <typeparam name="TRole">The type of the class representing a role</typeparam>
    public class RoleStore<TRole>:IRoleStore<TRole>
        where TRole : IdentityRole
    {
        private IRedisClient db;
        public static string AppNamespace;
        private bool Disposed;

        public RoleStore(IRedisClient _db)
        {
            db = _db;
            Disposed = false;
        }
       

        public Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            try
            {
                var redisStream = db.As<TRole>();
                redisStream.Store(role);
                return Task.FromResult(IdentityResult.Success);
            }
            catch(Exception ex)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError() { Description = ex.Message }));
            }
        }

        public Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            try
            {
                var redisStream = db.As<TRole>();
                redisStream.DeleteById(role.Id);
                return Task.FromResult(IdentityResult.Success);
            }
            catch (Exception ex)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError() { Description = ex.Message }));
            }
        }

        public void Dispose()
        {
            Disposed = true;
            //throw new NotImplementedException();
        }

        public Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(roleId))
            {
                throw new ArgumentNullException("role id is null");
            }
            try
            {
                var redisStream = db.As<TRole>();
                return Task.FromResult(redisStream.GetById(roleId));
            }
            catch //(Exception ex)
            {
                return Task.FromResult(default(TRole));
            }
        }

        public Task<TRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            try
            {
                var redisStream = db.As<TRole>();
                var data = from c in redisStream.GetAll()
                           where c.NormalizedName == normalizedRoleName
                           orderby c.Id
                           select c;
                return Task.FromResult(data.SingleOrDefault());
            }
            catch //(Exception ex)
            {
                return Task.FromResult(default(TRole));
            }
           
        }

        public Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            return Task.FromResult(role.NormalizedName);
        }

        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            return Task.FromResult(role.Id);
        }

        public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            return Task.FromResult(role.Name);
        }

        public Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            role.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            role.Name = roleName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            try
            {
                var redisStream = db.As<TRole>();
                role.ConcurrencyStamp = Guid.NewGuid().ToString();
                redisStream.Store(role);
                return Task.FromResult(IdentityResult.Success);
            }
            catch (Exception ex)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError() { Description = ex.Message }));
            }
            
        }
    }
    
}