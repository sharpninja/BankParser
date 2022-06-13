using System;

namespace BankParser.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);
}
