﻿using N_Tier.Core.Common;

namespace N_Tier.Core.Entities
{
    public class TodoList : BaseEntity, IAuditedEntity
    {
        public string Title { get; set; }

        public List<TodoItem> Items { get; } = new List<TodoItem>();

        public Guid CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public Guid UpdatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }
}
