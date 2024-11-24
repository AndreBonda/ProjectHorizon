namespace ImageOptimizerLambda.Exceptions;

public class TooLargeImageException(string message) : Exception(message);