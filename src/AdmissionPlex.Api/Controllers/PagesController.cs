using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AdmissionPlex.Core.Entities.Content;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    public PagesController(IUnitOfWork uow) => _uow = uow;

    [HttpGet]
    public async Task<IActionResult> GetPublished()
    {
        var pages = await _uow.Pages.GetPublishedAsync();
        return Ok(ApiResponse<object>.Ok(pages.Select(p => new
        { p.Id, p.Slug, p.Title, PageType = p.PageType.ToString(), p.PublishedAt, p.MetaDescription })));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var page = await _uow.Pages.GetBySlugAsync(slug);
        if (page == null) return NotFound(ApiResponse<object>.Fail("Page not found."));
        return Ok(ApiResponse<object>.Ok(new
        { page.Id, page.Slug, page.Title, page.Content, page.MetaTitle, page.MetaDescription, PageType = page.PageType.ToString() }));
    }

    [HttpGet("faqs")]
    public async Task<IActionResult> GetFaqs()
    {
        var faqs = await _uow.Pages.GetPublishedFaqsAsync();
        return Ok(ApiResponse<object>.Ok(faqs.Select(f => new { f.Id, f.Category, f.Question, f.Answer })));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PageCreateDto dto)
    {
        Enum.TryParse<PageType>(dto.PageType, true, out var pageType);
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var page = new Page
        {
            Slug = dto.Slug ?? dto.Title.ToLower().Replace(" ", "-"),
            Title = dto.Title, Content = dto.Content,
            MetaTitle = dto.MetaTitle, MetaDescription = dto.MetaDescription,
            PageType = pageType, IsPublished = dto.IsPublished,
            PublishedAt = dto.IsPublished ? DateTime.UtcNow : null, AuthorId = userId
        };
        await _uow.Pages.AddAsync(page);
        await _uow.SaveChangesAsync();
        return Created("", ApiResponse<object>.Ok(new { page.Id, page.Slug }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] PageCreateDto dto)
    {
        var page = await _uow.Pages.GetByIdAsync(id);
        if (page == null) return NotFound(ApiResponse<object>.Fail("Page not found."));
        page.Title = dto.Title; page.Content = dto.Content;
        page.MetaTitle = dto.MetaTitle; page.MetaDescription = dto.MetaDescription;
        page.IsPublished = dto.IsPublished;
        if (dto.IsPublished && !page.PublishedAt.HasValue) page.PublishedAt = DateTime.UtcNow;
        _uow.Pages.Update(page);
        await _uow.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { page.Id }));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var page = await _uow.Pages.GetByIdAsync(id);
        if (page == null) return NotFound(ApiResponse<object>.Fail("Page not found."));
        page.IsPublished = false;
        _uow.Pages.Update(page);
        await _uow.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Page unpublished."));
    }
}

public class PageCreateDto
{
    public string Title { get; set; } = "";
    public string? Slug { get; set; }
    public string Content { get; set; } = "";
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string PageType { get; set; } = "Static";
    public bool IsPublished { get; set; }
}
