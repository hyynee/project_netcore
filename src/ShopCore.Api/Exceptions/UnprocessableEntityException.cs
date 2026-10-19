namespace ShopCore.Api.Exceptions;

public sealed class UnprocessableEntityException(string message) : Exception(message);
