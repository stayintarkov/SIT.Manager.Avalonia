using FluentAvalonia.UI.Controls;
using System;

namespace SIT.Manager.Models;

public record NavigationItem(string Name, string ToolTip, Symbol Icon, Type NavigationTarget, string? Tag = null);
