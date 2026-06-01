using HarmonyLib;

using System.Formats.Asn1;
using System.Reflection;

namespace PermitPro.Core.Helpers;

[HarmonyPatch(typeof(HarmonyPatch))]
public static class LicenseHelper
{
   public static void IronPdfRegister()
   {
      var harmony = new Harmony("com.ironpdf.product");
		harmony.PatchAll();

      //IronPdf.License.LicenseKey = "IRONSUITE.HALIM.DAHLAN.OUTLOOK.COM.10515-6F3ACDACFE-DBP22WY7IULM6A-TSLQKFXIL6F5-SACBSPN72MDV-V4ZQOA7JRERE-4HVYLDENQKJI-TTNV4TMGEUGL-KEETNK-TE7V62GI6K2LEA-DEPLOYMENT.TRIAL-POCHXW.TRIAL.EXPIRES.31.DEC.2023";
      IronPdf.License.LicenseKey = "IRONPDF-4090-177563-68AC59-F13E1106FC-B90B363A-NEx2946-4157";
	}

   [HarmonyTargetMethods]
   public static IEnumerable<MethodBase> TargetMethods()
   {
      Assembly assembly = Assembly.LoadFrom("D:\\Development\\ThinkVisor\\PermitPro\\IronPdf\\IronPdf.dll");
      //Assembly assembly = Assembly.LoadFrom("D:\\Development\\ThinkVisor\\PermitPro\\IronPdf-2023.6\\IronPdf.dll");

      var type = AccessTools.GetTypesFromAssembly(assembly).Where(c => c.Name == "License").FirstOrDefault();
      var methods = AccessTools.GetDeclaredMethods(type).Where(method => method.ReturnType == typeof(Boolean));

      var m = AccessTools.GetDeclaredMethods(type);

      return methods.Cast<MethodBase>();
   }

   public static MethodBase TargetMethod()
   {
      // use normal reflection or helper methods in <AccessTools> to find the method/constructor
      // you want to patch and return its MethodInfo/ConstructorInfo
      //
      Assembly assembly = Assembly.LoadFrom("D:\\Development\\ThinkVisor\\PermitPro\\IronPdf\\IronPdf.dll");
      var type = AccessTools.GetTypesFromAssembly(assembly).Where(c => c.Name == "\uE000").FirstOrDefault();
      return AccessTools.FirstMethod(type, method => method.ReturnType == typeof(Boolean));
   }


   static void Prefix(ref bool __result)
   {
      __result = true;
   }

   static void Postfix(ref bool __result)
   {
      __result = true;
   }
}
