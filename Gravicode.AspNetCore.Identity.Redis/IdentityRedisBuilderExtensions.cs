// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Gravicode.AspNetCore.Identity.Redis;
using ServiceStack.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IdentityBuilder"/> for adding entity framework stores.
    /// </summary>
    public static class IdentityRedisBuilderExtensions
    {
        /// <summary>
        /// Adds an Entity Framework implementation of identity information stores.
        /// </summary>
        /// <typeparam name="TContext">The Entity Framework database context to use.</typeparam>
        /// <param name="builder">The <see cref="IdentityBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IdentityBuilder"/> instance this method extends.</returns>
        public static IdentityBuilder AddRedisStores(this IdentityBuilder builder, string RedisCon)

        {
            AddStores(builder.Services, builder.UserType, builder.RoleType, RedisCon);
          
            return builder;
        }
        
        private static void AddStores(IServiceCollection services, Type userType, Type roleType,string RedisCon)
        {
            /*
            var identityUserType = FindGenericBaseType(userType, typeof(IdentityUser));
            if (identityUserType == null)
            {
                throw new InvalidOperationException("Not identity user");
            }
            var identityRoleType = FindGenericBaseType(roleType, typeof(IdentityRole<,,>));
            if (identityRoleType == null)
            {
                throw new InvalidOperationException("not identity role");
            }*/
            var mgr = new PooledRedisClientManager(RedisCon);
            var client = mgr.GetClient();
            services.TryAddSingleton<IUserStore<IdentityUser>>(new UserStore<IdentityUser>(client));
            services.TryAddSingleton<IRoleStore<IdentityRole>>(new RoleStore<IdentityRole>(client));
            /*
            services.TryAddScoped(
                typeof(IUserStore<>).MakeGenericType(userType),
                typeof(UserStore<>).MakeGenericType(userType));
            services.TryAddScoped(
                typeof(IRoleStore<>).MakeGenericType(roleType),
                typeof(RoleStore<>).MakeGenericType(roleType));
            */
        }

        private static TypeInfo FindGenericBaseType(Type currentType, Type genericBaseType)
        {
            var type = currentType.GetTypeInfo();
            while (type.BaseType != null)
            {
                type = type.BaseType.GetTypeInfo();
                var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
                if (genericType != null && genericType == genericBaseType)
                {
                    return type;
                }
            }
            return null;
        }
    }
}