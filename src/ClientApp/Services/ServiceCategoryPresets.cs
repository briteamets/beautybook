namespace ClientApp.Services;

public static class ServiceCategoryPresets
{
    public record Preset(string En, string Et, bool IsOther = false);

    public static readonly Dictionary<string, List<Preset>> ByCategory =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Hair"] =
            [
                new("Haircut", "Juukselõikus"),
                new("Colour", "Juuste värvimine"),
                new("Highlights", "Tõsted"),
                new("Balayage", "Balayage"),
                new("Blowout", "Föönkuivatamine"),
                new("Toning", "Toonimine"),
                new("Keratin treatment", "Keratiinravi"),
                new("Updo", "Juukseseade"),
                new("Hair extensions", "Juuksepikendused"),
                new("Other", "Muu", IsOther: true)
            ],
            ["Nails"] =
            [
                new("Manicure", "Maniküür"),
                new("Pedicure", "Pediküür"),
                new("Gel nails", "Geelküüned"),
                new("Acrylic nails", "Akrüülküüned"),
                new("Nail art", "Küünekunst"),
                new("French manicure", "Prantsuse maniküür"),
                new("Nail removal", "Küünte eemaldamine"),
                new("Nail repair", "Küünte parandus"),
                new("Other", "Muu", IsOther: true)
            ],
            ["Lashes"] =
            [
                new("Lash lift", "Ripsmete koolutamine"),
                new("Lash tint", "Ripsmete värvimine"),
                new("Classic lashes", "Klassikalised ripsmed"),
                new("Volume lashes", "Volüümripsmed"),
                new("Mega volume", "Megavolüüm"),
                new("Lash removal", "Ripsmete eemaldamine"),
                new("Brow tint", "Kulmude värvimine"),
                new("Brow lamination", "Kulmude laminatsioon"),
                new("Other", "Muu", IsOther: true)
            ],
            ["Skin Care"] =
            [
                new("Facial", "Näohooldus"),
                new("Chemical peel", "Keemiline koorimine"),
                new("Microneedling", "Mikronõelimine"),
                new("LED therapy", "LED-teraapia"),
                new("Hydrafacial", "Hüdrofatsiaal"),
                new("Microdermabrasion", "Mikrodermabrasioon"),
                new("Acne treatment", "Akneravi"),
                new("Anti-aging", "Vananemisvastane ravi"),
                new("Other", "Muu", IsOther: true)
            ],
            ["Massage"] =
            [
                new("Swedish massage", "Rootsi massaaž"),
                new("Deep tissue", "Sügava koe massaaž"),
                new("Hot stone massage", "Kuumakivimassaaž"),
                new("Sports massage", "Spordimassaaž"),
                new("Relaxation massage", "Lõõgastav massaaž"),
                new("Lymphatic drainage", "Lümfidrenaaž"),
                new("Thai massage", "Tai massaaž"),
                new("Other", "Muu", IsOther: true)
            ]
        };
}
