using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ��� ������� � ��
using SoundTradeWebApp.Data;       // �������� ��
using SoundTradeWebApp.Models;      // ��� ErrorViewModel
using SoundTradeWebApp.Models.ViewModels; // ��� TrackIndexViewModel
using System.Diagnostics;
using System.Linq;                 // ��� .OrderByDescending, .Take, .Select
using System.Threading.Tasks;      // ��� Task<>

namespace SoundTradeWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // <-- ��������� �������� ��

        // ��������� ����������� ��� ��������� DbContext
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // <-- �������� �����������
        }

        // ��������� ����� Index
        public async Task<IActionResult> Index()
        {
            // ��������� N ��������� ������ ��� ������� "�����������"
            int numberOfRecommendedTracks = 6; // ������� ������ ����������

            var recommendedTracks = await _context.Tracks
                                        .AsNoTracking() // ������ ��� ������
                                        .OrderByDescending(t => t.UploadDate) // ��������� �� ���� (������� �����)
                                        .Take(numberOfRecommendedTracks)       // ����� ������ N
                                        .Select(t => new TrackIndexViewModel // ���������� � ViewModel
                                        {
                                            Id = t.Id,
                                            Title = t.Title,
                                            ArtistName = t.ArtistName,
                                            Genre = t.Genre,
                                            // ���������� URL ��� ���������������
                                            AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                            // ��������� ���� (VocalType, Mood, Lyrics, UploadDate) ����� ��������, ���� ��� ����� �� �������
                                        })
                                        .ToListAsync();

            // �������� ������ ��������������� ������ � �������������
            return View(recommendedTracks);
        }

        // ����� Error �������� ��� ���������
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            _logger.LogError($"Error Occurred. TraceId: {errorViewModel.RequestId}");
            return View(errorViewModel);
        }
    }
}