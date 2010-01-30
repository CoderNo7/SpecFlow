﻿using System;
using System.Collections.Generic;     
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gherkin;
using TechTalk.SpecFlow.Parser.GherkinBuilder;
using TechTalk.SpecFlow.Parser.SyntaxElements;

namespace TechTalk.SpecFlow.Parser
{
    public class SpecFlowLangParser
    {
        private readonly CultureInfo defaultLanguage;

        public SpecFlowLangParser(CultureInfo defaultLanguage)
        {
            this.defaultLanguage = defaultLanguage;
        }

        public Feature Parse(TextReader featureFileReader, string sourceFileName)
        {
            var feature = Parse(featureFileReader);
            feature.SourceFile = Path.GetFullPath(sourceFileName);
            return feature;
        }

        static private readonly Regex languageRe = new Regex(@"^\s*#\s*language:\s*(?<lang>[\w-]+)\s*\n");

        public Feature Parse(TextReader featureFileReader)
        {
            var fileContent = featureFileReader.ReadToEnd() + Environment.NewLine;

            CultureInfo language = GetLanguage(fileContent);

            var gherkinListener = new GherkinListener();

            var lexer = GetLexer(language, gherkinListener);
            using (var reader = new StringReader(fileContent)) lexer.Scan(reader);

            if (gherkinListener.Errors.Length > 0)
            {
                throw new SpecFlowParserException("Invalid Gherkin file!", gherkinListener.Errors);
            }

            Feature feature = gherkinListener.GetResult();

            if (feature == null)
                throw new SpecFlowParserException("Invalid Gherkin file!");

            feature.Language = language.Name;

            return feature;
        }

        private IEnumerable<string> GetPossibleLanguageNames(CultureInfo language)
        {
            return GetParentChain(language).SelectMany(lang => new [] {GetGherkinLangName(lang.Name), GetGherkinLangName(lang.Name.Replace("-", ""))});
        }

        private IEnumerable<CultureInfo> GetParentChain(CultureInfo cultureInfo)
        {
            var current = cultureInfo;
            yield return current;
            while (current.Parent != current)
            {
                current = current.Parent;
                yield return current;
            }

        }

        private ILexer GetLexer(CultureInfo language, IListener listener)
        {
            return Lexers.Create(GetPossibleLanguageNames(language).First(Lexers.Exists), listener);
        }

        private CultureInfo GetLanguage(string fileContent)
        {
            CultureInfo language = defaultLanguage;

            var langMatch = languageRe.Match(fileContent);
            if (langMatch.Success)
            {
                string langName = langMatch.Groups["lang"].Value;
                langName = ResolveLangNameExceptions(langName);
                language = new CultureInfo(langName);
            }
            return language;
        }

        private string GetGherkinLangName(string isoLangName)
        {
            switch (isoLangName)
            {
                case "sv":
                    return "se";
                default:
                    return isoLangName;
            }
        }

        private string ResolveLangNameExceptions(string langName)
        {
            switch (langName)
            {
                case "se":
                    return "sv";
                default:
                    return langName;
            }
        }
    }
}
