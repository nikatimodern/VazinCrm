using CRM.Core.Domain.Carizos.Enum;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Carizos.Entities
{
    public class Carizo : BaseEntity
    {
        public string Title { get; set; }
        public string Date { get; set; }
        public DateTime? DateMiladi { get; set; }
        public string Description { get; set; }
        public bool? Repeat { get; set; }
        public string RepeatType { get; set; }
        public string RepeatTypeTxt { get; set; }
        public string RepeatDays { get; set; }
        public string RepeatUntil { get; set; }
        public DateTime RepeatUntilMiladi { get; set; }
        public bool? Remind { get; set; }
        public string RemindTime { get; set; }
        public bool RemindDismiss { get; set; }
        public bool TaskStatus { get; set; }
        public string ColorTask { get; set; }
        public List<TaskUser> TaskUsers { get; set; }
        public bool TaskHide { get; set; } = false;

        [NotMapped]
        public string ElapsTime { get; set; }


        public string AssignUser { get; set; } // نام کاربری که تسک برای او میباشد
        public string AssignUserId { get; set; }// آیدی کاربری که تسک برای او میباشد


        public string CompleteTaskUser { get; set; }//اگر استراکی بود توسط چه کسی انجام شده
        public string CompleteTaskUserId { get; set; }//اگر استراکی بود توسط چه کسی انجام شده


        public TaskTypeEnum TaskType { get; set; }//نوع تسک اختصاصی اشتراکی تکی
        public string TaskTypeTxt { get; set; }//نوع تسک اختصاصی اشتراکی تکی


        public Guid? ShareTaskId { get; set; }//اگر تسک اشتراکی بودآیدی تسک اصلی در اینجا ذخیره می شود

        public string EntityType { get; set; }//ارتباط تسک با جداول هست ممکنه تسک مربوط به یک ابجکتی باشه مثل ثبت واریزی 
        public Guid? EntityId { get; set; }//آیدی آبجکت وصل شده به تسک

        public bool Systematic { get; set; } = false;



        public Carizo Clone()
        {
            return new Carizo
            {
                Title = this.Title,
                Date = this.Date,
                DateMiladi = this.DateMiladi,
                Description = this.Description,
                Repeat = this.Repeat,
                RepeatType = this.RepeatType,
                RepeatTypeTxt = this.RepeatTypeTxt,
                RepeatDays = this.RepeatDays,
                RepeatUntil = this.RepeatUntil,
                RepeatUntilMiladi = this.RepeatUntilMiladi,
                Remind = this.Remind,
                RemindTime = this.RemindTime,
                RemindDismiss = this.RemindDismiss,
                TaskStatus = this.TaskStatus,
                TaskHide = this.TaskHide,
                ColorTask = this.ColorTask,
                TaskType = this.TaskType,
                TaskTypeTxt = this.TaskTypeTxt,
                ShareTaskId = this.ShareTaskId,
                //TaskUsers = new List<TaskUser>(this.TaskUsers), // کپی مستقل لیست
                TaskUsers = null, // کپی مستقل لیست
                UserCreatorID = this.UserCreatorID,
                UserCreator = this.UserCreator,
                UserModifyID = this.UserModifyID,
                UserModify = this.UserModify,
                CommentText = this.CommentText,
                EntityId = this.EntityId,
                EntityType = this.EntityType
            };
        }






    }







}
