Gravicode.AspNetCore.Identity.Redis
=====================

ASP.NET Core Identity provider that uses Redis for storage and works with VS2017 and VSCode

## Purpose ##

Provide library that enable dev to use Redis as ASP.Net Core Identity storage

## Instructions ##
These instructions assume you know how to set up Redis within an ASP.Net Core MVC application.

1. Create a new ASP.NET Core Web Application, choose the Individual User Accounts authentication type.
2. Remove the Entity Framework Core packages and replace with Redis Identity:

```PowerShell
Uninstall-Package Microsoft.AspNetCore.Identity.EntityFrameworkCore
Install-Package Gravicode.AspNetCore.Identity.Redis
```

3. In

	~/Startup.cs

    * Remove the namespace: Microsoft.AspNetCore.Identity.EntityFrameworkCore and Microsoft.EntityFrameworkCore
    * Add the namespace: Gravicode.AspNetCore.Identity.Redis and Microsoft.AspNetCore.Identity

4. In ~/Startup.cs

```C#
static Startup(){
	...
        public void ConfigureServices(IServiceCollection services)
        {
            var RedisConStr = Configuration.GetConnectionString("RedisConnectionString");

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddRedisStores(RedisConStr)
                .AddDefaultTokenProviders();

            UserStore<IdentityUser>.AppNamespace = "urn:app:";

            // Add framework services.
            services.AddMvc();

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

        }
}
```

5. Delete 'Data' folder

6. Delete ~/Models/ApplicationUser.cs

7. Replace all class "ApplicationUser" with "IdentityUser" in all files

8. Add namespace Gravicode.AspNetCore.Identity.Redis to ~/Controllers/AccountController.cs and ~/Controllers/ManageController.cs and ~/Controllers/_ViewImports.cshtml

9. Remove unnecessary namespace in ~/Controllers/AccountController.cs and ~/Controllers/ManageController.cs and ~/Controllers/_ViewImports.cshtml

## Thanks To ##

Special thanks to [Aminjam for Redis.AspNet.Identity](https://github.com/aminjam/Redis.AspNet.Identity) that gave me the base for starting the Redis provider for ASP.Net Core Provider
