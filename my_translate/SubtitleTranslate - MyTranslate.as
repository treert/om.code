/*
	Real time subtitle translate for PotPlayer using MyTranslate API
*/

// void OnInitialize()
// void OnFinalize()
// string GetTitle() 														-> get title for UI
// string GetVersion														-> get version for manage
// string GetDesc()														-> get detail information
// string GetLoginTitle()													-> get title for login dialog
// string GetLoginDesc()													-> get desc for login dialog
// string GetUserText()														-> get user text for login dialog
// string GetPasswordText()													-> get password text for login dialog
// string ServerLogin(string User, string Pass)								-> login
// string ServerLogout()													-> logout
//------------------------------------------------------------------------------------------------
// array<string> GetSrcLangs() 													-> get source language
// array<string> GetDstLangs() 													-> get target language
// string Translate(string Text, string &in SrcLang, string &in DstLang) 	-> do translate !!

array<string> StrLangTable = {
    "en","ja","ko","zh",
};

array<string> TgtLangTable = 
{
    "zh",
	"sq",
	"ar",
	"az",
	"bn",
	"bg",
	"ca",
	"cs",
	"da",
	"nl",
	"en",
	"eo",
	"et",
	"fi",
	"fr",
	"de",
	"el",
	"he",
	"hi",
	"hu",
	"id",
	"ga",
	"it",
	"ja",
	"ko",
	"lv",
	"lt",
	"ms",
	"nb",
	"fa",
	"pl",
	"pt",
	"ro",
	"ru",
	"sr",
	"sk",
	"sl",
	"es",
	"sv",
	"tl",
	"th",
	"tr",
	"ur",
	"uk",
	"vi",
};

string UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";

string GetTitle()
{
	return "My Translate";
}

string GetVersion()
{
	return "1";
}

string GetDesc()
{
	return "My Translate";
}

string GetLoginTitle()
{
	return "Input http url";
}

string GetLoginDesc()
{
	return "Input http url";
}

string GetUserText()
{
	return "http url:";
}

string GetPasswordText()
{
	return "";
}

string input_url = "";

string ServerLogin(string User, string Pass)
{
	input_url = User;
	return "200 ok";
}

void ServerLogout()
{
	// input_url = "";
}

array<string> GetSrcLangs()
{
	array<string> ret = StrLangTable;
	
	ret.insertAt(0, ""); // empty is auto
	return ret;
}

array<string> GetDstLangs()
{
	array<string> ret = TgtLangTable;
	
	return ret;
}

string JsonParseNew(string json)
{
	JsonReader Reader;
	JsonValue Root;
	string ret = "";	
	
	if (Reader.parse(json, Root) && Root.isObject())
	{
		JsonValue translatedText = Root["translation"];
		
		if (translatedText.isString())
		{
			ret = translatedText.asString();
		}
		else
		{
			JsonValue error = Root["error"];
			if (error.isString())
			{
				ret = error.asString();
			}
		}
	} 
	return ret;
}

string Translate(string Text, string &in SrcLang, string &in DstLang)
{
//HostOpenConsole();	// for debug

    if (SrcLang.length() <= 0) SrcLang = "auto";
    SrcLang.MakeLower();

    
    string url = "http://127.0.0.1:5000/translate";
    if (input_url.length() > 0){
        url = input_url;
    }
    string header = "accept: application/json\r\nContent-Type: application/x-www-form-urlencoded\r\n";	
    string post;
    string enc_text = HostUrlEncode(Text);
    post += "text=" + enc_text + "&";
    post += "tgt_lang=" + DstLang + "&";
    post += "src_lang=" + SrcLang;

    string text = HostUrlGetString(url, UserAgent, header, post);
    string ret = JsonParseNew(text);		
    if (ret.length() > 0)
    {
        SrcLang = "UTF8";
        DstLang = "UTF8";
        return ret;
    }
    return "";
}
