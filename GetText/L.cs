using System.ComponentModel;
using System.IO;
using SecondLanguage;

// ReSharper disable once CheckNamespace
public static class L
{
    public static Translator Translator = new Translator();
    public static string _([Localizable(false)]string template)
    {
        return Translator.Translate(template);
    }

    public static string _([Localizable(false)]string template, params object[] args)
    {
        return Translator.Translate(template, args);
    }

    public static void SetupFromData(byte[] po)
    {
        po = po ?? new byte[0];
        var translation = new GettextPOTranslation();
        translation.Load(po);
        Translator = new Translator();
        Translator.RegisterTranslation(translation);
    }

    public static void SetupFromFile(string po)
    {
        if (po == null)
            SetupFromData(null);
        SetupFromData(File.ReadAllBytes(po));
    }
}