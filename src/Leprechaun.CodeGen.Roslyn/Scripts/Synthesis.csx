﻿// Generates Synthesis models

Log.Debug($"Emitting Synthesis templates for {ConfigurationName}...");

public string RenderTemplates()
{
	var localCode = new System.Text.StringBuilder();

	foreach (var template in Templates)
	{
		localCode.AppendLine($@"
namespace {template.Namespace}
{{
	using global::Sitecore.ContentSearch;
	using global::Sitecore.Data;
	using global::Sitecore.Data.Items;
	using global::System.CodeDom.Compiler;
	using global::System.Collections.Generic;
	using Synthesis;
	using Synthesis.FieldTypes;
	using Synthesis.FieldTypes.Interfaces;
	using Synthesis.Initializers;
	using Synthesis.Synchronization;
	using System.CodeDom.Compiler;	

	/// <summary>Controls the appearance of the inheriting template in site navigation.</summary>
	[RepresentsSitecoreTemplateAttribute(""{{{template.Id}}}"", """", ""{ConfigurationName}"")]	
	[GeneratedCode(""Leprechaun"", ""2.0.0.0"")]
	public interface I{template.CodeName}Item : {GetBaseInterfaces(template)}
	{{
		{RenderInterfaceFields(template)}
	}}

	/// <summary>Controls the appearance of the inheriting template in site navigation.</summary>
	[GeneratedCode(""Leprechaun"", ""2.0.0.0"")]
	public class {template.CodeName} : StandardTemplateItem, I{template.CodeName}Item
	{{
		public {template.CodeName}(Item innerItem) : base(innerItem)
		{{
		}}

		public {template.CodeName}(IDictionary<string, string> searchFields) : base(searchFields)
		{{
		}}

		/// <summary>The name of the Sitecore Template that this class represents</summary>
		public static string TemplateName => ""{template.Name}"";

		/// <summary>The ID of the Sitecore Template that this class represents</summary>
		public static ID ItemTemplateId => new ID(""{{{template.Id}}}"");

		/// <summary>The ID of the Sitecore Template that this class represents</summary>
		public override ID TemplateId => ItemTemplateId;

		{RenderFields(template)}
	}}

	[GeneratedCode(""Leprechaun"", ""2.0.0.0"")]
	public class {template.CodeName}Initializer : ITemplateInitializer
	{{
		public ID InitializesTemplateId => new ID(""{{{template.Id}}}"");

		public IStandardTemplateItem CreateInstance(Item innerItem)
		{{
			return new {template.CodeName}(innerItem);
		}}

		public IStandardTemplateItem CreateInstanceFromSearch(IDictionary<string, string> searchFields)
		{{
			return new {template.CodeName}(searchFields);
		}}
	}}
}}"
		);
	}

	return localCode.ToString();
}

Code.AppendLine($@"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
// ReSharper disable All

{RenderTemplates()}
");

public string GetBaseInterfaces(TemplateCodeGenerationMetadata template)
{
	var bases = new System.Collections.Generic.List<string>(template.BaseTemplates.Count + 1);

	foreach(var baseTemplate in template.BaseTemplates) 
	{
		bases.Add($"global::{baseTemplate.Namespace}.I{baseTemplate.CodeName}Item");
	}

	if (bases.Count == 0)
	{
		// IStandardTemplateItem only needed when no other bases exist otherwise irrelevant by transitive inheritance
		bases.Add("IStandardTemplateItem");
	}

	return string.Join(", ", bases);
}

public string RenderInterfaceFields(TemplateCodeGenerationMetadata template)
{
	var localCode = new System.Text.StringBuilder();

	foreach (var field in template.OwnFields)
	{
		localCode.AppendLine($@"
		/// <summary>{field.HelpText}</summary>
		[IndexFieldAttribute(""{GetSearchFieldName(field)}"")]
		I{GetFieldType(field)} {field.CodeName} {{ get; }}");
	}

	return localCode.ToString();
}

public string RenderFields(TemplateCodeGenerationMetadata template)
{
	var localCode = new System.Text.StringBuilder();

	foreach (var field in template.AllFields)
	{
		localCode.AppendLine($@"
		private {GetFieldType(field)} {GetBackingFieldName(field)};
		/// <summary>{field.HelpText}</summary>
		[IndexFieldAttribute(""{GetSearchFieldName(field)}"")]
		public I{GetFieldType(field)} {field.CodeName} => {GetBackingFieldName(field)} ?? ({GetBackingFieldName(field)} = new {GetFieldType(field)}(new LazyField(() => InnerItem.Fields[""{{{field.Id}}}""], ""{template.Path}"", ""{field.Name}""), GetSearchFieldValue(""{GetSearchFieldName(field)}"")));
		");
	}

	return localCode.ToString();
}

public string GetFieldType(TemplateFieldCodeGenerationMetadata field)
{
	switch(field.Type) {
		// Simple Types
		case "Checkbox": return "BooleanField";
		case "Date": return "DateTimeField";
		case "Datetime": return "DateTimeField";
		case "File": return "FileField";
		case "Image": return "ImageField";
		case "Integer": return "IntegerField";
		case "Number": return "NumericField";
		case "Rich Text": return "RichTextField";
		case "Multi-Line Text":
		case "Password":
		case "Single-Line Text": return "TextField";

		// List Types
		case "Checklist":
		case "Grouped Droplink": return "ItemReferenceField";
		case "Droplist":
		case "Grouped Droplist": return "TextField";
		case "Multilist":
		case "Multilist with Search":
		case "Treelist":
		case "TreelistEx": return "ItemReferenceListField";
		case "Name Value List": return "DictionaryField";

		// Link Types
		case "Droplink":
		case "Droptree": return "ItemReferenceField";
		case "General Link": return "HyperlinkField";

		// System Types
		case "Internal Link": return "PathItemReferenceField";

		// Developer Types
		case "Tristate": return "TristateField";

		// Deprecated Types
		case "text": return "TextField";
		case "memo": return "TextField";
		case "lookup":
		case "reference":
		case "tree": return "ItemReferenceField";
		case "tree list": return "ItemReferenceListField";
		case "html": return "RichTextField";
		case "link": return "HyperlinkField";
		default: return "TextField";
	}
}

public string GetBackingFieldName(TemplateFieldCodeGenerationMetadata field)
{
	return $"_{field.CodeName.Substring(0, 1).ToLowerInvariant()}{field.CodeName.Substring(1)}";
}

public string GetSearchFieldName(TemplateFieldCodeGenerationMetadata field)
{
	// not using Solr? Remove the GetSolrFieldTypeSuffix(field.Type)
	return field.Name.Replace(" ", "_").ToLowerInvariant() + GetSolrFieldTypeSuffix(field.Type);
}

public string GetSolrFieldTypeSuffix(string typeName)
{
	switch (typeName.ToLower())
	{
		case "checkbox":
			return "_b";
		case "date":
		case "datetime":
			return "_tdt";
		case "checklist":
		case "treelist":
		case "treelist with search":
		case "treelistex":
		case "multilist":
		case "multilist with search":
		case "tags":
			return "_sm";
		case "droplink":
		case "droptree":
			return "_s";
		case "general link":
		case "general link with search":
		case "text":
		case "single-line text":
		case "multi-line text":
		case "rich text":
			return "_t";
		case "number":
			return "_tf";
		case "integer":
			return "_tl";
		default:
			return string.Empty;
	}
}
