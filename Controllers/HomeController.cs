using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ��� ������� � ��
using SoundTradeWebApp.Data;       // �������� ��
using SoundTradeWebApp.Models;      // ��� ErrorViewModel
using SoundTradeWebApp.Models.ViewModels; // ��� TrackIndexViewModel � IndexPageViewModel
using System.Diagnostics;
using System.Linq;                 // ��� .OrderByDescending, .Take, .Select
using System.Threading.Tasks;      // ��� Task<>
using System.Collections.Generic; // ��� IEnumerable

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
            int numberOfTracks = 6; // ������� ������ ���������� � ������ ������

            var viewModel = new IndexPageViewModel();

            // ��������� N ��������� ������ ��� ������� "�����������"
            viewModel.RecommendedTracks = await _context.Tracks
                                        .AsNoTracking() // ������ ��� ������
                                        .OrderByDescending(t => t.UploadDate) // ��������� �� ���� (������� �����)
                                        .Take(numberOfTracks)       // ����� ������ N
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

            // ��������� N ��������� ������ ��� ������� "��� ������"
            viewModel.PopTracks = await _context.Tracks
                                    .AsNoTracking()
                                    .Where(t => t.Genre == "Pop") // ��������� �� ����� "���"
                                    .OrderByDescending(t => t.UploadDate)
                                    .Take(numberOfTracks)
                                    .Select(t => new TrackIndexViewModel
                                    {
                                        Id = t.Id,
                                        Title = t.Title,
                                        ArtistName = t.ArtistName,
                                        Genre = t.Genre,
                                        AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                    })
                                    .ToListAsync();

            // ��������� N ��������� ������ ��� ������� "��� ������"
            viewModel.RockTracks = await _context.Tracks
                                    .AsNoTracking()
                                    .Where(t => t.Genre == "Rock") // ��������� �� ����� "���"
                                    .OrderByDescending(t => t.UploadDate)
                                    .Take(numberOfTracks)
                                    .Select(t => new TrackIndexViewModel
                                    {
                                        Id = t.Id,
                                        Title = t.Title,
                                        ArtistName = t.ArtistName,
                                        Genre = t.Genre,
                                        AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                    })
                                    .ToListAsync();

            // ��������� N ��������� ������ ��� ������� "���-��� ������"
            viewModel.HipHopTracks = await _context.Tracks
                                        .AsNoTracking()
                                        .Where(t => t.Genre == "HipHop") // ��������� �� ����� "���-���"
                                        .OrderByDescending(t => t.UploadDate)
                                        .Take(numberOfTracks)
                                        .Select(t => new TrackIndexViewModel
                                        {
                                            Id = t.Id,
                                            Title = t.Title,
                                            ArtistName = t.ArtistName,
                                            Genre = t.Genre,
                                            AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                        })
                                        .ToListAsync();

            // ��������� N ��������� ������ ��� ������� "���� ������"
            viewModel.JazzTracks = await _context.Tracks
                                    .AsNoTracking()
                                    .Where(t => t.Genre == "Jazz") // ��������� �� ����� "����"
                                    .OrderByDescending(t => t.UploadDate)
                                    .Take(numberOfTracks)
                                    .Select(t => new TrackIndexViewModel
                                    {
                                        Id = t.Id,
                                        Title = t.Title,
                                        ArtistName = t.ArtistName,
                                        Genre = t.Genre,
                                        AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                    })
                                    .ToListAsync();

            // ��������� N ��������� ������ ��� ������� "����������� ������"
            viewModel.ElectronicTracks = await _context.Tracks
                                            .AsNoTracking()
                                            .Where(t => t.Genre == "Electronic") // ��������� �� ����� "����������� ������"
                                            .OrderByDescending(t => t.UploadDate)
                                            .Take(numberOfTracks)
                                            .Select(t => new TrackIndexViewModel
                                            {
                                                Id = t.Id,
                                                Title = t.Title,
                                                ArtistName = t.ArtistName,
                                                Genre = t.Genre,
                                                AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                            })
                                            .ToListAsync();


            // �������� ViewModel � ������� ���� ������ � �������������
            return View(viewModel);
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