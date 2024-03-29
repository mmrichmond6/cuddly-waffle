﻿using HouseholdManager.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HouseholdManager.Models.ViewModels;

namespace HouseholdManager.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            //Mission table
            var allMissions = _context.Missions.Include(t => t.Room)
                                               .Include(u => u.Member);
            List<MissionViewModel> model = await (from mission in allMissions
                                                  select new MissionViewModel(mission))
                                                  .ToListAsync();
            int CountAll = allMissions.Count();
            ViewBag.AllMissions = model;

            //Doughnut Chart-Missions done by Contributor
            ViewBag.DoughnutChartData = allMissions
                .GroupBy(j => j.Member.MemberId)
                .Select(k => new
                {
                    memberNameWithIcon = k.First().Member.Icon + " " + k.First().Member.UserName,
                    amount = k.Sum(j => j.Point),
                    formattedAmount = k.Sum(j => j.Point).ToString("0"),
                })
                .ToList();

            return View();
        }
    }
}
