namespace LibNode.Api.Exceptions;

/// <summary>
/// Изображение превышает допустимый бюджет пикселей (защита от decompression-bomb).
/// Контроллеры медиа преобразуют это в HTTP 400.
/// </summary>
public class ImageTooLargeException : Exception
{
    public ImageTooLargeException(string message) : base(message)
    {
    }
}
