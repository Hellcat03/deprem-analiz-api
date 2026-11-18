using Deprem.Application;
using FluentValidation;

public class OlayKontrol : AbstractValidator<Olay>
{
    public OlayKontrol()
    {
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Enlem).InclusiveBetween(-90, 90);
        RuleFor(x => x.Boylam).InclusiveBetween(-180, 180);
        RuleFor(x => x.Yer).NotEmpty().MaximumLength(250);
        RuleFor(x => x.BuyuklukTuru).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Saglayici).NotEmpty().MaximumLength(80);
        RuleFor(x => x.KaynakKimligi).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OlusZamani).Must(t => t != default && t <= DateTimeOffset.UtcNow.AddMinutes(1)).WithMessage("Geçerli bir olay zamanı girin.");
    }
}
public class YapiKontrol : AbstractValidator<Yapi>
{
    public YapiKontrol()
    {
        RuleFor(x => x.Ad).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Konum).NotEmpty().MaximumLength(250);
        RuleFor(x => x.BetonSinifi).Matches("^C[1-9][0-9]{0,2}(/[1-9][0-9]{0,2})?$");
        RuleFor(x => x.KatSayisi).InclusiveBetween(1, 250);
        RuleFor(x => x.YapimYili).InclusiveBetween(1000, DateTime.UtcNow.Year);
        RuleFor(x => x.TasiyiciSistem).MaximumLength(200);
        RuleFor(x => x.Yonetmelik).MaximumLength(200);
        RuleFor(x => x.Belgeler).NotNull();
        RuleForEach(x => x.Belgeler).Must(x => YapiInceleme.Belgeler.Contains(x)).WithMessage("Belge türü tanınmadı.");
    }
}
