using System;
using CRM.Core.Domain.QualityControls.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.QualityControls.Configs;

public sealed class QualityControlQuestionConfig : IEntityTypeConfiguration<QualityControlQuestion>
{
    public void Configure(EntityTypeBuilder<QualityControlQuestion> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Title).HasMaxLength(250).IsRequired();
        builder.Property(item => item.QuestionText).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.ZeroAnswerText).HasMaxLength(250).IsRequired();
        builder.Property(item => item.OneAnswerText).HasMaxLength(250).IsRequired();
        builder.HasQueryFilter(item => !item.IsDeleted);
        builder.HasData(
            CreateQuestion("45c326dd-4500-4cf5-94ce-000000000001", "مغایرت در کیفیت محصول",
                "آیا در کیفیت محصول دریافتی (مثل ضخامت، جنس، چاپ، دوخت یا رنگ) مغایرتی با سفارشتان وجود داشت؟",
                "خیر (سالم)", "بله (دارای مغایرت)", 1),
            CreateQuestion("45c326dd-4500-4cf5-94ce-000000000002", "مغایرت در مقدار / وزن",
                "آیا در تعداد کارتن‌ها، وزن بار یا متراژ تحویلی، مغایرت یا کسری داشتید؟",
                "خیر (کامل)", "بله (کسری/اضافه)", 2),
            CreateQuestion("45c326dd-4500-4cf5-94ce-000000000003", "مغایرت در زمان تحویل",
                "آیا بار طبق زمان توافق‌شده تحویل شد یا تأخیر نامتعارف داشت؟",
                "خیر، به‌موقع بود", "بله، تأخیر داشت", 3));
    }

    private static QualityControlQuestion CreateQuestion(string id, string title, string text, string zeroText, string oneText, int sortOrder)
    {
        var seededAt = new DateTime(2026, 11, 2);
        return new QualityControlQuestion {
            Id = Guid.Parse(id), Title = title, QuestionText = text, ZeroAnswerText = zeroText, OneAnswerText = oneText,
            SortOrder = sortOrder, IsActive = true, IsDeleted = false,
            CreateDate = seededAt, ModifyDate = seededAt,
            CreateDateTxt = "1405/08/11 00:00:00", ModifyDateTxt = "1405/08/11 00:00:00",
            UserCreatorID = "system", UserCreator = "System", UserModifyID = "system", UserModify = "System", CommentText = string.Empty
        };
    }
}
