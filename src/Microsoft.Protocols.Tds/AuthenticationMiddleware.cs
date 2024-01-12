using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds;

public static class AuthenticationExtensions
{
    public static ITdsConnectionBuilder UseAuthentication(this ITdsConnectionBuilder builder)
        => builder.Use(async (ctx, next) =>
        {
            var feature = ctx.Features.Get<IAuthenticationFeature>();

            if (feature is { IsAuthenticated: true })
            {
                await next(ctx);
            }
            else
            {
                if (feature is null)
                {
                    feature = new DefaultAuthentication(ctx);
                    ctx.Features.Set<IAuthenticationFeature>(feature);
                }

                await feature.AuthenticateAsync();
                await next(ctx);
            }
        });

    private sealed class DefaultAuthentication(TdsConnectionContext context) : IAuthenticationFeature
    {
        public bool IsAuthenticated { get; set; }

        async ValueTask IAuthenticationFeature.AuthenticateAsync()
        {
            await context.SendPacketAsync(TdsType.PreLogin);
            await context.ReadPacketAsync(TdsType.PreLogin);

            var ssl = context.Features.GetRequiredFeature<ISslFeature>();

            if (ssl is { IsEnabled: not true })
            {
                await ssl.EnableAsync();
            }

            await context.SendPacketAsync(TdsType.Login7);

            if (ssl is { })
            {
                await ssl.DisableAsync();
            }

            await context.ReadPacketAsync(TdsType.Login7);
        }
    }
}
