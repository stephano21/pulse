using System.Net;

namespace Pulse.Application.Email;

/// <summary>
/// Plantilla HTML única para todos los correos transaccionales (confirmación de cuenta, etc.):
/// layout con tablas (compatibilidad con Outlook) y estilos inline (los clientes de correo
/// ignoran &lt;style&gt; en el &lt;head&gt; casi siempre). Colores de marca de Yapa (la app que
/// usa el destinatario), no los del panel admin interno.
/// </summary>
public static class EmailTemplate
{
    private const string Verde = "#7a9b6e";
    private const string Texto = "#1a1a1a";
    private const string TextoSuave = "#5c5c5c";
    private const string Fondo = "#f0f2f0";
    private const string Borde = "#e5e5e5";

    /// <param name="heading">Título dentro de la tarjeta (texto plano, se escapa solo).</param>
    /// <param name="bodyHtml">Cuerpo ya en HTML (el llamador controla el marcado, ej. &lt;p&gt;).</param>
    /// <param name="ctaText">Texto del botón. Null = sin botón.</param>
    /// <param name="ctaUrl">Destino del botón; también se muestra como enlace de respaldo.</param>
    public static string Render(string heading, string bodyHtml, string? ctaText = null, string? ctaUrl = null)
    {
        var safeHeading = WebUtility.HtmlEncode(heading);
        var year = DateTimeOffset.UtcNow.Year;

        var ctaBlock = "";
        if (!string.IsNullOrWhiteSpace(ctaText) && !string.IsNullOrWhiteSpace(ctaUrl))
        {
            var safeCtaText = WebUtility.HtmlEncode(ctaText);
            ctaBlock =
                $"""
                 <table role="presentation" cellpadding="0" cellspacing="0" style="margin:28px 0 4px;">
                   <tr>
                     <td style="border-radius:10px;background-color:{Verde};">
                       <a href="{ctaUrl}" style="display:inline-block;padding:13px 30px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:10px;">{safeCtaText}</a>
                     </td>
                   </tr>
                 </table>
                 <p style="margin:8px 0 0;font-size:12px;line-height:1.6;color:#9a9a9a;word-break:break-all;">
                   Si el botón no funciona, copiá y pegá este enlace en tu navegador:<br>
                   <a href="{ctaUrl}" style="color:{Verde};">{ctaUrl}</a>
                 </p>
                 """;
        }

        return $"""
                <!doctype html>
                <html lang="es">
                <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>{safeHeading}</title>
                </head>
                <body style="margin:0;padding:0;background-color:{Fondo};font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:{Fondo};padding:32px 16px;">
                    <tr>
                      <td align="center">
                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background-color:#ffffff;border-radius:16px;overflow:hidden;border:1px solid {Borde};">
                          <tr>
                            <td style="background-color:{Verde};padding:24px 32px;">
                              <span style="font-size:20px;font-weight:700;color:#ffffff;letter-spacing:0.3px;">Yapa</span>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:32px;">
                              <h1 style="margin:0 0 16px;font-size:20px;font-weight:700;color:{Texto};">{safeHeading}</h1>
                              <div style="font-size:15px;line-height:1.6;color:{TextoSuave};">{bodyHtml}</div>
                              {ctaBlock}
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:18px 32px;background-color:{Fondo};border-top:1px solid {Borde};">
                              <p style="margin:0;font-size:12px;color:#9a9a9a;line-height:1.5;">Si no esperabas este correo, podés ignorarlo con tranquilidad.</p>
                            </td>
                          </tr>
                        </table>
                        <p style="margin:20px 0 0;font-size:12px;color:#a5a5a5;">© {year} Yapa</p>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;
    }
}
