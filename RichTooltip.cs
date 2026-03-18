using System;
using System.Numerics;
using ImGuiNET;

namespace WhatsATooltip;

/// <summary>
/// Immediate-mode rich tooltip renderer using ImGui DrawList inside BeginTooltip/EndTooltip.
/// Zero-allocation static API  - call only when IsItemHovered() is true.
/// All parameters are primitives/IntPtr so delegates cross PluginBridge safely.
/// </summary>
public static class RichTooltip
{
    // ── Internal state (static, no allocation) ──────────────────────
    private static bool _isOpen;
    private static uint _accent;
    private static uint _accentDim;
    private static uint _label;
    private static uint _desc;
    private static uint _cardBg;
    private static float _currentY;
    private static float _contentX;
    private static float _maxWidth;
    private static ImDrawListPtr _dl;

    private const float Pad = 10f;
    private const float LineGap = 2f;

    // ── Core lifecycle ──────────────────────────────────────────────

    /// <summary>
    /// Begin a rich tooltip. Call only when ImGui.IsItemHovered() is true.
    /// </summary>
    public static bool Begin(uint accent, uint accentDim, uint label, uint desc, uint cardBg, float maxWidth)
    {
        _accent = accent;
        _accentDim = accentDim;
        _label = label;
        _desc = desc;
        _cardBg = cardBg;
        _maxWidth = maxWidth;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4f);
        ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0, 0, 0, 0));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));

        ImGui.BeginTooltip();

        _dl = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();
        _contentX = winPos.X + Pad;
        _currentY = winPos.Y + Pad;

        // Draw card background (overdrawn at correct size in End)
        var bgMax = new Vector2(winPos.X + maxWidth + Pad * 2, winPos.Y + 600);
        _dl.AddRectFilled(winPos, bgMax, cardBg, 4f);
        _dl.AddRect(winPos, bgMax, WithAlpha(accent, 0.35f), 4f, ImDrawFlags.None, 1f);

        _isOpen = true;
        return true;
    }

    /// <summary>
    /// End the rich tooltip. Must match a Begin() call.
    /// </summary>
    public static void End()
    {
        if (!_isOpen) return;

        float totalH = _currentY - ImGui.GetWindowPos().Y + Pad;
        float totalW = _maxWidth + Pad * 2;

        // Redraw only the border at the correct size  - the background was already
        // drawn oversized in Begin(). We must NOT AddRectFilled here because it
        // would cover all content drawn between Begin() and End().
        var winPos = ImGui.GetWindowPos();
        _dl.AddRect(winPos, new Vector2(winPos.X + totalW, winPos.Y + totalH),
            WithAlpha(_accent, 0.35f), 4f, ImDrawFlags.None, 1f);

        ImGui.SetCursorScreenPos(winPos);
        ImGui.Dummy(new Vector2(totalW, totalH));

        ImGui.EndTooltip();
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(2);

        _isOpen = false;
    }

    // ── Content blocks ──────────────────────────────────────────────

    /// <summary>
    /// Draw a header with optional icon and accent-colored title text.
    /// </summary>
    public static void Header(string text, IntPtr iconTexId, float iconSize)
    {
        if (!_isOpen) return;

        float textX = _contentX;

        if (iconTexId != IntPtr.Zero)
        {
            var iconMin = new Vector2(_contentX, _currentY);
            var iconMax = new Vector2(_contentX + iconSize, _currentY + iconSize);
            _dl.AddImage(iconTexId, iconMin, iconMax);
            textX = _contentX + iconSize + 6f;
        }

        float textY = _currentY;
        if (iconTexId != IntPtr.Zero)
        {
            var textSize = ImGui.CalcTextSize(text);
            textY = _currentY + (iconSize - textSize.Y) * 0.5f;
        }

        _dl.AddText(new Vector2(textX, textY), _accent, text);

        float rowH = iconTexId != IntPtr.Zero ? iconSize : ImGui.CalcTextSize(text).Y;
        _currentY += rowH + LineGap + 2f;
    }

    /// <summary>
    /// Draw a pill-shaped colored badge (e.g. tier name).
    /// </summary>
    public static void Badge(string text, uint color)
    {
        if (!_isOpen) return;

        var textSize = ImGui.CalcTextSize(text);
        float padX = 8f, padY = 2f;
        var min = new Vector2(_contentX, _currentY);
        var max = new Vector2(_contentX + textSize.X + padX * 2, _currentY + textSize.Y + padY * 2);

        _dl.AddRectFilled(min, max, WithAlpha(color, 0.2f), 8f);
        _dl.AddRect(min, max, WithAlpha(color, 0.5f), 8f, ImDrawFlags.None, 1f);
        _dl.AddText(new Vector2(min.X + padX, min.Y + padY), color, text);

        _currentY = max.Y + LineGap + 2f;
    }

    /// <summary>
    /// Draw a horizontal separator line.
    /// </summary>
    public static void Separator()
    {
        if (!_isOpen) return;

        _currentY += 2f;
        _dl.AddLine(
            new Vector2(_contentX, _currentY),
            new Vector2(_contentX + _maxWidth, _currentY),
            WithAlpha(_accent, 0.25f), 1f);
        _currentY += 4f;
    }

    /// <summary>
    /// Draw a stat line: label on the left, value right-aligned.
    /// </summary>
    public static void StatLine(string label, string value, uint valueColor)
    {
        if (!_isOpen) return;

        _dl.AddText(new Vector2(_contentX, _currentY), _desc, label);

        var valSize = ImGui.CalcTextSize(value);
        float valX = _contentX + _maxWidth - valSize.X;
        _dl.AddText(new Vector2(valX, _currentY), valueColor, value);

        _currentY += ImGui.CalcTextSize(label).Y + LineGap;
    }

    /// <summary>
    /// Draw word-wrapped body text in subdued color.
    /// </summary>
    public static void Body(string text)
    {
        if (!_isOpen) return;
        DrawWrappedText(text, _desc);
    }

    /// <summary>
    /// Draw word-wrapped body text in label (bright) color.
    /// </summary>
    public static void BodyBright(string text)
    {
        if (!_isOpen) return;
        DrawWrappedText(text, _label);
    }

    /// <summary>
    /// Draw a bullet point: small circle + indented text.
    /// </summary>
    public static void Bullet(string text, uint dotColor)
    {
        if (!_isOpen) return;

        float textH = ImGui.CalcTextSize(text).Y;
        float dotY = _currentY + textH * 0.5f;

        _dl.AddCircleFilled(new Vector2(_contentX + 4f, dotY), 3f, dotColor);
        _dl.AddText(new Vector2(_contentX + 14f, _currentY), _label, text);

        _currentY += textH + LineGap;
    }

    // ── Typed convenience helpers ───────────────────────────────────

    /// <summary>
    /// Full tooltip for a CrowdControl entry: icon, name, tier badge, tracked buff names.
    /// </summary>
    public static void ForCcEntry(string name, string tier, string[] buffNames,
        IntPtr iconTexId, uint tierColor,
        uint accent, uint accentDim, uint label, uint desc, uint cardBg)
    {
        if (!Begin(accent, accentDim, label, desc, cardBg, 320f)) return;

        Header(name, iconTexId, 32f);
        Badge(tier, tierColor);
        Separator();

        if (buffNames.Length > 0)
        {
            Body("Tracked buff names:");
            foreach (var buff in buffNames)
                Bullet(buff, accent);
        }

        End();
    }

    /// <summary>
    /// Full tooltip for a live buff entry: internal name, display name, timer, tracking status.
    /// </summary>
    public static void ForLiveBuff(string internalName, string displayName,
        float timer, int charges, bool isTracked,
        uint accent, uint accentDim, uint label, uint desc, uint cardBg)
    {
        if (!Begin(accent, accentDim, label, desc, cardBg, 280f)) return;

        Header(displayName.Length > 0 ? displayName : internalName, IntPtr.Zero, 0f);

        if (displayName.Length > 0 && displayName != internalName)
            Body(internalName);

        Separator();

        if (timer > 0)
            StatLine("Timer", $"{timer:F1}s", label);
        if (charges > 0)
            StatLine("Charges", charges.ToString(), label);

        if (isTracked)
            Badge("Tracked", WithAlpha(accent, 1f));
        else
            Body("Click to add as custom CC");

        End();
    }

    // ── Internal helpers ────────────────────────────────────────────

    private static void DrawWrappedText(string text, uint color)
    {
        if (string.IsNullOrEmpty(text))
            return;

        float lineH = ImGui.CalcTextSize("A").Y;
        float spaceW = ImGui.CalcTextSize(" ").X;
        float lineX = _contentX;
        float lineEnd = _contentX + _maxWidth;

        var words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (word.Length == 0) continue;

            var wordSize = ImGui.CalcTextSize(word);

            if (lineX + wordSize.X > lineEnd && lineX > _contentX)
            {
                _currentY += lineH + 1f;
                lineX = _contentX;
            }

            _dl.AddText(new Vector2(lineX, _currentY), color, word);
            lineX += wordSize.X + spaceW;
        }

        _currentY += lineH + LineGap;
    }

    private static uint WithAlpha(uint color, float alpha)
    {
        var v = ImGui.ColorConvertU32ToFloat4(color);
        v.W = alpha;
        return ImGui.GetColorU32(v);
    }
}
