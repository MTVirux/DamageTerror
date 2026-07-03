global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Numerics;
global using System.Threading;
global using System.Threading.Tasks;

global using Dalamud.Bindings.ImGui;
global using Dalamud.Bindings.ImPlot;
global using Dalamud.Interface;
global using Dalamud.Plugin.Services;
global using ImGui = Dalamud.Bindings.ImGui.ImGui;

global using Newtonsoft.Json;
global using Newtonsoft.Json.Converters;
global using Newtonsoft.Json.Linq;

global using DamageTerror;
global using DamageTerror.Core;
global using DamageTerror.Enums;
global using DamageTerror.Gui;
global using DamageTerror.Gui.ConfigWindow;
global using DamageTerror.Gui.MainWindow;
global using DamageTerror.Gui.MainWindow.Detail;
global using DamageTerror.Helpers;
global using DamageTerror.Jobs;
global using DamageTerror.Models;
global using DamageTerror.Presets;
global using DamageTerror.Services;

global using ECommons.DalamudServices;
global using ECommons.GameHelpers;
