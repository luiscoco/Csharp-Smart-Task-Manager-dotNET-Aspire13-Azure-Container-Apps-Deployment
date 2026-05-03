using System;
using SmartTaskManager.Domain.Common;

namespace SmartTaskManager.Domain.Entities;

public sealed class Category : BaseEntity
{
    public Category(string name, string description = "")
        : this(Guid.NewGuid(), name, description)
    {
    }

    public Category(Guid id, string name, string description = "")
        : base(id)
    {
        Name = ValidateName(name);
        Description = NormalizeDescription(description);
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public void Rename(string newName)
    {
        Name = ValidateName(newName);
    }

    public void ChangeDescription(string description)
    {
        Description = NormalizeDescription(description);
    }

    public static Category CreatePersonalDefault()
    {
        return new Category("Personal", "Default category for personal tasks.");
    }

    public static Category CreateWorkDefault()
    {
        return new Category("Work", "Default category for work-related tasks.");
    }

    public static Category CreateLearningDefault()
    {
        return new Category("Learning", "Default category for learning tasks.");
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required.");
        }

        return name.Trim();
    }

    private static string NormalizeDescription(string description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : description.Trim();
    }
}
