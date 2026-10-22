using ShopCore.Application.Catalog;

namespace ShopCore.UnitTests.Catalog;

public sealed class ProductSearchTokenizerTests
{
    [Fact]
    public void Tokenize_SplitsOnPunctuationAndLowercasesTokens()
    {
        var terms = ProductSearchTokenizer.Tokenize("Wireless-Headphones, Pro!");

        Assert.Equal(["wireless", "headphones", "pro"], terms);
    }

    [Fact]
    public void Tokenize_DropsSingleCharacterTokens()
    {
        var terms = ProductSearchTokenizer.Tokenize("T-Shirt size M");

        Assert.Equal(["shirt", "size"], terms);
    }

    [Fact]
    public void Tokenize_KeepsVietnameseLettersAndLowercasesThem()
    {
        var terms = ProductSearchTokenizer.Tokenize("Điện thoại NOKIA");

        Assert.Equal(["điện", "thoại", "nokia"], terms);
    }

    [Fact]
    public void Tokenize_ReturnsDistinctTermsOnly()
    {
        var terms = ProductSearchTokenizer.Tokenize("Áo áo Áo");

        Assert.Equal(["áo"], terms);
    }

    [Fact]
    public void Tokenize_ReturnsNoTermsForBlankOrSymbolOnlyInput()
    {
        Assert.Empty(ProductSearchTokenizer.Tokenize(null));
        Assert.Empty(ProductSearchTokenizer.Tokenize("   "));
        Assert.Empty(ProductSearchTokenizer.Tokenize("%%%"));
    }

    [Fact]
    public void Tokenize_KeepsDigitsInTerms()
    {
        var terms = ProductSearchTokenizer.Tokenize("iPhone 15 Pro Max 256GB");

        Assert.Equal(["iphone", "15", "pro", "max", "256gb"], terms);
    }
}
