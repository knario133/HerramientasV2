using HerramientasV2.ToolWindows.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace HerramientasV2.ToolWindows
{
    public static class CodeGenerator
    {
        public static string GenerateHtml(string formTitle, bool isResponsive, string responsiveSize, bool hasBorder, bool isStriped, bool isHover, bool isSmall, int columnCount, List<DataRow> dataRows, List<BotonRow> buttonRows)
        {
            var html = $"<div>\n\t<h1 class=\"display-4\">{formTitle}</h1>\n";
            if (isResponsive)
            {
                html += $"\t<div class=\"table-responsive-{responsiveSize}\">\n";
            }

            var tableClass = $"table {(hasBorder ? "table-bordered " : "")}{(isStriped ? "table-striped " : "")}{(isHover ? "table-hover " : "")}{(isSmall ? "table-sm " : "")}";
            html += $"\t<table class=\"{tableClass}\">\n\t\t<tbody>\n";

            var rowCount = (int)Math.Ceiling((double)dataRows.Count / columnCount);
            for (int i = 0; i < rowCount; i++)
            {
                html += "\t\t\t<tr>\n";
                for (int j = 0; j < columnCount; j++)
                {
                    var index = i * columnCount + j;
                    if (index < dataRows.Count)
                    {
                        var row = dataRows[index];
                        html += $"\t\t\t\t<td>\n\t\t\t\t\t<asp:Label ID=\"LB_{FormatString(row.NombreCampo)}\" runat=\"server\" AssociatedControlID=\"TB_{FormatString(row.NombreCampo)}\" class=\"col-form-label\" Text=\"{FormatString(row.NombreCampo, true)}\"></asp:Label>\n\t\t\t\t</td>\n";
                        html += $"\t\t\t\t<td>\n\t\t\t\t\t<asp:TextBox ID=\"TB_{FormatString(row.NombreCampo)}\" CssClass=\"form-control\" TextMode=\"{row.TipoCampo}\" runat=\"server\" {(row.EsRequerido ? "readonly" : "")}></asp:TextBox>\n";
                        html += $"\t\t\t\t\t<asp:HiddenField ID=\"HF_{FormatString(row.NombreCampo)}\" runat=\"server\" />\n\t\t\t\t</td>\n";
                    }
                    else
                    {
                        html += "\t\t\t\t<td></td>\n\t\t\t\t<td></td>\n";
                    }
                }
                html += "\t\t\t</tr>\n";
            }

            html += "\t\t</tbody>\n\t</table>\n";
            if (isResponsive)
            {
                html += "\t</div>\n";
            }

            html += "\t<div style=\"text-align: right;\">\n";
            foreach (var button in buttonRows)
            {
                if (button.LlamaJS || button.LlamaJSSeleccionado)
                {
                    html += $"\t\t<input type=\"button\" ID=\"BTN_{FormatString(button.NombreCampo)}\" class=\"{button.TipoCampo}\" onclick=\"FN_{FormatString(button.NombreCampo)}_Click()\" value=\"{FormatString(button.NombreCampo, true)}\" />\n";
                }
                else
                {
                    html += $"\t\t<asp:Button ID=\"BTN_{FormatString(button.NombreCampo)}\" runat=\"server\" CssClass=\"{button.TipoCampo}\" Text=\"{FormatString(button.NombreCampo, true)}\" />\n";
                }
            }
            html += "\t</div>\n";

            html += GenerateJavaScript(dataRows, buttonRows);

            html += "</div>\n";
            return html;
        }

        private static string GenerateJavaScript(List<DataRow> dataRows, List<BotonRow> buttonRows)
        {
            var js = "<script type=\"application/javascript\">\n";
            foreach (var button in buttonRows.Where(b => b.LlamaJS || b.LlamaJSSeleccionado))
            {
                js += $"function FN_{FormatString(button.NombreCampo)}_Click() {{\n";
                foreach (var row in dataRows)
                {
                    js += $"\tvar {FormatString(row.NombreCampo)} = document.getElementById('<%=TB_{FormatString(row.NombreCampo)}.ClientID%>').value;\n";
                }

                if (button.swall == "Dialogo Confirmación")
                {
                    js += GetSwalConfirmationDialog(button.LlamaJSSeleccionado ? GetFunctionCall(dataRows) : "");
                }
                else if (button.LlamaJSSeleccionado)
                {
                    js += GetFunctionCall(dataRows);
                }

                if (button.swall == "Alerta Post")
                {
                    js += "Swal.fire({ title: \"Confirmacion\", text: \"Mensaje\", icon: \"success\" });\n";
                }

                js += "}\n";
            }
            js += "</script>\n";
            return js;
        }

        private static string GetSwalConfirmationDialog(string functionCall)
        {
            return @"
    Swal.fire({
        title: ""Titulo"",
        text: ""Texto"",
        icon: ""warning"",
        showCancelButton: true,
        confirmButtonColor: ""#3085d6"",
        cancelButtonColor: ""#d33"",
        confirmButtonText: ""TextoBotonConfirmar""
    }).then((result) => {
        if (result.isConfirmed) {
            " + functionCall + @"
        }
    });
";
        }

        private static string GetFunctionCall(List<DataRow> dataRows)
        {
            var parameters = string.Join(", ", dataRows.Select(r => FormatString(r.NombreCampo)));
            return $"NombreFuncionJS({parameters});\n";
        }

        private static string FormatString(string input, bool toTitleCase = false)
        {
            var result = Regex.Replace(input.ToLower(), @"[^a-zA-Z0-9]+", " ");
            if (toTitleCase)
            {
                result = new CultureInfo("en-US", false).TextInfo.ToTitleCase(result);
            }
            return result.Replace(" ", "");
        }
    }
}
