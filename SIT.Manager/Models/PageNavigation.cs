using System;

namespace SIT.Manager.Models;

public record PageNavigation(Type TargetPage, bool SuppressTransition = false);
