namespace CRM.Core.Domain.RequestAmounts.Enums;

public enum RequestStatusEnum {
    ثبت_اولیه = 0,
    در_انتظار_تایید_مدیرعامل = 1,
    در_انتظار_ارسال_به_مالی = 2,
    در_انتظار_پرداخت = 3,
    پرداخت_شده = 4,
}