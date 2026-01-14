using MediatR;

namespace Enterprise.Application.Features.Products.Commands.UploadProductImage;

public record UploadProductImageCommand(
    Guid ProductId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<UploadProductImageResponse>;

public record UploadProductImageResponse(string ImageUrl, string ImagePath);
