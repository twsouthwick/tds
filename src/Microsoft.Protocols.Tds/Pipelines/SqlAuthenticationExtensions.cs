// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;

namespace Microsoft.Protocols.Tds;

public static class SqlAuthenticationExtensions
{
    public static ITdsConnectionBuilder UseSqlAuthentication(this ITdsConnectionBuilder builder)
        => builder.Use((ctx, next) =>
        {
            var c = ctx.Features.GetRequiredFeature<IConnectionStringFeature>();

            var hasUsername = c.TryGetValue("User Id", out var userId);
            var hasPassword = c.TryGetValue("Password", out var password);

            if (hasUsername && hasPassword)
            {
                ctx.Features.Set<ISqlUserAuthenticationFeature>(new SqlAuthentication(userId, password.ToString()));
            }

            return next(ctx);
        });

    private sealed class SqlAuthentication : ISqlUserAuthenticationFeature
    {
        public SqlAuthentication(ReadOnlyMemory<char> userName, string password)
        {
            var domainIndex = userName.Span.IndexOf('\\');

            if (domainIndex == -1)
            {
                UserName = userName.ToString();
                HostName = string.Empty;
            }
            else
            {
                UserName = userName.Slice(0, domainIndex).ToString();
                HostName = userName.Slice(domainIndex).ToString();
            }

            Password = password;
        }

        public string HostName { get; }

        public string UserName { get; }

        public string Password { get; }
    }
}
