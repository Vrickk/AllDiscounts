using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TermWeb.Data;
using TermWeb.Models;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Http;
using reCAPTCHA.AspNetCore;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace TermWeb.Controllers
{
    

    [RequireHttps]
    public class ArticlesController : Controller
    {
        private readonly TermWebContext _context;
        private readonly RecaptchaKeyModel _recaptchaSettings;
        private PasswordHasher<Article> passwordHasher = new PasswordHasher<Article>();

        public ArticlesController(TermWebContext context, IOptions<RecaptchaKeyModel> recaptchaSettings, IConfiguration configuration)
        {
            _context = context;
            _recaptchaSettings = recaptchaSettings.Value;
            // IConfiguration을 사용하여 appsettings.json에서 직접 RecaptchaSettings를 가져옵니다.
            var recaptchaSettingsFromConfig = configuration.GetSection("RecaptchaSettings").Get<RecaptchaKeyModel>();
        }


        // GET: Articles
        public async Task<IActionResult> Index(string category, string searchString)
        {
            if (_context.Article == null)
            {
                return Problem("Entity set 'TermWebContext.Article'  is null.");
            }

            IQueryable<string> genreQuery = from m in _context.Article
                                            orderby m.Head
                                            select m.Head;

            var articles = from m in _context.Article
                         select m;

            articles = articles.OrderByDescending(s => s.PostDate);

            if (!String.IsNullOrEmpty(searchString))
            {
                articles = articles.Where(s => s.Title!.Contains(searchString));
            }

            if (!String.IsNullOrEmpty(category))
            {
                articles = articles.Where(x => x.Head == category);
            }

            foreach (var article in articles)
            {
                if (article.Deadline < DateTime.Now) article.IsStillGoing = false;
                else article.IsStillGoing = true;

                TimeSpan ts = article.Deadline.Subtract(DateTime.Now);

                
            }




            var categoryVM = new CategoryViewModel
            {
                Category = new SelectList(await genreQuery.Distinct().ToListAsync()),
                Articles = await articles.ToListAsync()
            };

            return View(categoryVM);
        }

        // GET: Articles/Details/5
        public async Task<IActionResult> Details(int? id, bool like)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Article
                .FirstOrDefaultAsync(m => m.Id == id);
            if (article == null)
            {
                return NotFound();
            }

            if (article.Deadline < DateTime.Now) article.IsStillGoing = false;
            else article.IsStillGoing = true;

            TimeSpan ts = article.Deadline.Subtract(DateTime.Now);

            

            return View(article);
        }

        public bool ReCaptchaPassed(string gRecaptchaResponse)
        {
            HttpClient httpClient = new HttpClient();

            var secretKey = _recaptchaSettings.SecretKey;

            var res = httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
            { "secret", secretKey },
            { "response", gRecaptchaResponse }
                })).Result;

            if (res.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            string JSONres = res.Content.ReadAsStringAsync().Result;
            dynamic JSONdata = JObject.Parse(JSONres);

            if (JSONdata.success == "true")
            {
                return true;
            }

            return false;
        }

        // GET: Articles/Create
        public IActionResult Create()
        {
            return View();
        }

        private string HashPassword(string password)
        {
            // 비밀번호 해싱 로직 추가
            // 예시: ASP.NET Identity의 PasswordHasher 사용

            return passwordHasher.HashPassword(null, password);
        }


        public bool IsValidInput(string input)
        {
            // 특정 특수문자를 사용할 수 없도록 검증
            if (input.Contains('"') || input.Contains('\'') || input.Contains('-') ||
                input.Contains('|') || input.Contains('[') || input.Contains(']') ||
                input.Contains('{') || input.Contains('}') || input.Contains('(') ||
                input.Contains(')') || input.Contains('='))
            {
                return false;
            }

            return true;
        }


        // POST: Articles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Kudos,Head,IsStillGoing,WriterID,MallName,Price,PostDate,DeliverPrice,Etc,Currency,Deadline,,Password,OrigPrice,Link,Discount,ConfirmPassword, Recaptcha")] Article article)
        {

            if (article.Password == null)
            {
                
                    ModelState.AddModelError(string.Empty, "비밀번호를 입력하세요.");
                    return View(article);
            }

            if (article.ConfirmPassword == null)
            {

                ModelState.AddModelError(string.Empty, "비밀번호를 입력하세요.");
                return View(article);
            }

            if (article.WriterID != null)
            {
                if (!IsValidInput(article.WriterID))
                {
                    ModelState.AddModelError(string.Empty, "ID에는 다음과 같은 특수문자를 사용할 수 없습니다: ', \" , -, |, [, {, ], }, (, ), = ");
                    return View(article);
                }
            }

            if (article.Password != null)
            {
                if (!IsValidInput(article.Password))
                {
                    ModelState.AddModelError(string.Empty, "비밀번호에는 다음과 같은 특수문자를 사용할 수 없습니다: ', \" , -, |, [, {, ], }, (, ), = ");
                    return View(article);
                }
            }
            
            if (article.ConfirmPassword != null)
            {
                if (!IsValidInput(article.ConfirmPassword))
                {
                    ModelState.AddModelError(string.Empty, "비밀번호에는 다음과 같은 특수문자를 사용할 수 없습니다: ', \" , -, |, [, {, ], }, (, ), = ");
                    return View(article);
                }
            }

           

            if (article.Password != article.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "비밀번호가 일치하지 않습니다.");
                return View(article);
            }

            article.Password = HashPassword(article.Password);

            if (ModelState.IsValid)
            {

                if (!ReCaptchaPassed(article.Recaptcha))
                {
                    ModelState.AddModelError(string.Empty, "reCAPTCHA 검증에 실패했습니다.");
                    return View(article);
                }

                else
                {
                    article.Discount = (article.OrigPrice - article.Price) / article.OrigPrice * 100;

                    if (article.Deadline < DateTime.Now) article.IsStillGoing = false;
                    else article.IsStillGoing = true;

                    TimeSpan ts = article.Deadline.Subtract(DateTime.Now);


                    _context.Add(article);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                
            }
            return View(article);
        }

        // GET: Articles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Article.FindAsync(id);
            if (article == null)
            {
                return NotFound();
            }
            return View(article);
        }

        // POST: Articles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Head,IsStillGoing,WriterID,MallName,Price,PostDate,DeliverPrice,Etc,Currency,Deadline,Password,OrigPrice,Link,Discount,EditPassword,FailedEditAttempts,Recaptcha")] Article article)
        {
            if (id != article.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (article.EditPassword == null)
                {

                    ModelState.AddModelError(string.Empty, "비밀번호를 입력하세요.");
                    return View(article);
                }

                // 비밀번호 비교 또는 필요한 경우 다른 유효성 검사를 수행합니다.
                // 기존 엔터티에서 해당 id의 article을 읽어옵니다.
                var existingArticle = await _context.Article.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

                // Check if the post is locked for editing
                if (existingArticle.LockedUntil.HasValue && existingArticle.LockedUntil > DateTime.Now)
                {
                    ModelState.AddModelError(string.Empty, $"글이 {existingArticle.LockedUntil.Value}까지 편집이 금지되어 있습니다.");
                    return View(article);
                }

                // 기존 엔터티의 해시된 비밀번호를 읽어옵니다.
                string existingPasswordHash = existingArticle.Password;

                // 사용자가 입력한 비밀번호와 기존 비밀번호 비교
                var result = passwordHasher.VerifyHashedPassword(article, existingPasswordHash, article.EditPassword);

                if (article.EditPassword == null)
                {
                    ModelState.AddModelError("EditPassword", "작성 시 입력했던 비밀번호를 입력해 주십시오.");
                    return View(article);
                }

                if (article.ConfirmPassword != null)
                {
                    if (!IsValidInput(article.EditPassword))
                    {
                        ModelState.AddModelError(string.Empty, "비밀번호에는 다음과 같은 특수문자를 사용할 수 없습니다: ', \" , -, |, [, {, ], }, (, ), = ");
                        return View(article);
                    }
                }
                
                if (result != PasswordVerificationResult.Success)
                {
                    // 비밀번호가 일치하지 않으면 에러 처리
                    ModelState.AddModelError(string.Empty,"기존 비밀번호가 일치하지 않습니다.");

                    // Increase the failed edit attempts
                   article.FailedEditAttempts = existingArticle.FailedEditAttempts + 1;

                    // Check if the user has reached the maximum failed attempts
                    if (article.FailedEditAttempts >= 3)
                    {
                        // Lock the post for editing for a certain period (e.g., 5 minutes)
                        article.LockedUntil = DateTime.Now.AddMinutes(5);
                        ModelState.AddModelError(string.Empty, $"글이 {article.LockedUntil.Value}까지 편집이 금지되어 있습니다.");

                        // Save only FailedEditAttempts and LockedUntil to update them
                        // Explicitly set FailedEditAttempts and LockedUntil properties
                        _context.Entry(article).Property(a => a.FailedEditAttempts).IsModified = true;
                        _context.Entry(article).Property(a => a.LockedUntil).IsModified = true;

                        await _context.SaveChangesAsync();

                        return View(article);
                    }

                    _context.Entry(article).Property(a => a.FailedEditAttempts).IsModified = true;
                    _context.Entry(article).Property(a => a.LockedUntil).IsModified = true;

                    await _context.SaveChangesAsync();

                    return View(article);
                }

                else
                {
                    // Check if the post is locked for editing
                    if (existingArticle.LockedUntil.HasValue && existingArticle.LockedUntil > DateTime.Now)
                    {
                        ModelState.AddModelError(string.Empty, $"글이 {existingArticle.LockedUntil.Value}까지 편집이 금지되어 있습니다.");
                        return View(article);
                    }

                    else
                    {
                        if (!ReCaptchaPassed(article.Recaptcha))
                        {
                            ModelState.AddModelError(string.Empty, "reCAPTCHA 검증에 실패했습니다.");
                            return View(article);
                        }

                        else
                        {
                            article.Password = existingPasswordHash;
                            article.Discount = (article.OrigPrice - article.Price) / article.OrigPrice * 100;

                            if (article.Deadline < DateTime.Now) article.IsStillGoing = false;
                            else article.IsStillGoing = true;

                            // 비밀번호 일치 시 실패한 시도 초기화
                            article.FailedEditAttempts = 0;
                            article.LockedUntil = null;
                        }
                        

                    }
                }

                

                // 나머지 로직은 이전과 동일


                try
                {
                    // 새 값으로 기존 엔터티를 업데이트합니다.
                    _context.Update(article);

                    // 변경 사항 저장
                    await _context.SaveChangesAsync();

                    // 편집이 성공하면 Index 페이지로 리디렉션합니다.
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArticleExists(article.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(article);
        }
        // GET: Articles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Article
                .FirstOrDefaultAsync(m => m.Id == id);
            if (article == null)
            {
                return NotFound();
            }

            if (article.Deadline < DateTime.Now) article.IsStillGoing = false;
            else article.IsStillGoing = true;

            TimeSpan ts = article.Deadline.Subtract(DateTime.Now);

            

            return View(article);
        }

        // POST: Articles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, [Bind("Id, EditPassword")] Article articles)
        {
            


            if (!IsValidInput(articles.EditPassword))
            {
                ModelState.AddModelError(string.Empty, "비밀번호에는 다음과 같은 특수문자를 사용할 수 없습니다: ', \" , -, |, [, {, ], }, (, ), = ");
                return View(articles);
            }

            // 비밀번호 비교 또는 필요한 경우 다른 유효성 검사를 수행합니다.
            // 기존 엔터티에서 해당 id의 article을 읽어옵니다.
            var existingArticle = await _context.Article.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

            // 기존 엔터티의 해시된 비밀번호를 읽어옵니다.
            string existingPasswordHash = existingArticle.Password;

            // 사용자가 입력한 비밀번호와 기존 비밀번호 비교
            var result = passwordHasher.VerifyHashedPassword(articles, existingPasswordHash, articles.EditPassword);

            if (existingArticle.LockedUntil.HasValue && existingArticle.LockedUntil > DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, $"글이 {existingArticle.LockedUntil.Value}까지 편집이 금지되어 있습니다.");
                return View(articles);
            }

            if (result != PasswordVerificationResult.Success)
            {
                // 비밀번호가 일치하지 않으면 에러 처리 또는 리다이렉트 등을 수행
                ModelState.AddModelError("EditPassword", "기존 비밀번호가 일치하지 않습니다.");


                // Increase the failed edit attempts
                articles.FailedEditAttempts = existingArticle.FailedEditAttempts + 1;

                // Check if the user has reached the maximum failed attempts
                if (articles.FailedEditAttempts >= 3)
                {
                    // Lock the post for editing for a certain period (e.g., 5 minutes)
                    articles.LockedUntil = DateTime.Now.AddMinutes(5);
                    ModelState.AddModelError(string.Empty, $"글이 {articles.LockedUntil.Value}까지 편집이 금지되어 있습니다.");

                    // Save only FailedEditAttempts and LockedUntil to update them
                    // Explicitly set FailedEditAttempts and LockedUntil properties
                    _context.Entry(articles).Property(a => a.FailedEditAttempts).IsModified = true;
                    _context.Entry(articles).Property(a => a.LockedUntil).IsModified = true;

                    await _context.SaveChangesAsync();

                    return View(articles);
                }

                _context.Entry(articles).Property(a => a.FailedEditAttempts).IsModified = true;
                _context.Entry(articles).Property(a => a.LockedUntil).IsModified = true;

                await _context.SaveChangesAsync();

                return View(articles);
            }

            else
            {
                // Check if the post is locked for editing
                if (existingArticle.LockedUntil.HasValue && existingArticle.LockedUntil > DateTime.Now)
                {
                    ModelState.AddModelError(string.Empty, $"글이 {existingArticle.LockedUntil.Value}까지 편집이 금지되어 있습니다.");
                    return View(articles);
                }

                else
                {
                    if (!ReCaptchaPassed(articles.Recaptcha))
                    {
                        ModelState.AddModelError(string.Empty, "reCAPTCHA 검증에 실패했습니다.");
                        return View(articles);
                    }

                    else
                    {
                        // 비밀번호 일치 시 실패한 시도 초기화
                        articles.FailedEditAttempts = 0;
                        articles.LockedUntil = null;

                    }
                }
            }

            _context.Article.Remove(articles);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private bool ArticleExists(int id)
        {
            return _context.Article.Any(e => e.Id == id);
        }

    }
}
