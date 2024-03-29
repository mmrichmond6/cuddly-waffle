﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HouseholdManager.Models;
using HouseholdManager.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HouseholdManager.Areas.Identity.Data;
using HouseholdManager.Data.API;
using HouseholdManager.Data.Interfaces;
using HouseholdManager.Models.ViewModels;

namespace HouseholdManager.Controllers
{
    [Authorize]
    public class MemberController : Controller, IRequestIcons
    {
        private readonly ApplicationDbContext _context;

        public MemberController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Member
        public async Task<IActionResult> Index()
        {
            var dataQuery = _context.Members.Include(t => t.Household)
                                            .Include(s => s.User);
            List<MemberViewModel> model = await (from member in dataQuery
                                                 select new MemberViewModel(member))
                                                 .ToListAsync();
            return View(model);
        }


        // GET: Member/AddOrEdit
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            PopulateHouseholds();
            PopulateIdentityUsers();
            await PopulateIcons();
            if (id == 0)
                return View(new Member());
            else
                return View(_context.Members.Find(id));
        }

        // POST: Member/AddOrEdit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AddOrEdit([Bind("MemberId,MemberType,Icon,HouseholdId,UserName,Completed")] Member member)
        {
            if (ModelState.IsValid)
            {
                //setting up IdentityUser relationship
                try
                {
                    IdentityUser? user = await (from u in _context.Users
                                                where u.UserName == member.UserName
                                                select u).FirstOrDefaultAsync();
                    if (user is null)
                    {
                        throw new ArgumentException("Attempted to link Member to invalid IdentityUser.");
                    }
                    member.User = user;
                }
                catch (ArgumentException e)
                {
                    return Problem(detail: e.Message, 
                                   statusCode: StatusCodes.Status400BadRequest);
                }

                if (member.MemberId == 0)
                    _context.Add(member);
                else
                    _context.Update(member);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            PopulateHouseholds();
            PopulateIdentityUsers();
            await PopulateIcons();
            return View(member);
        }


        // POST: Member/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Members == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Member'  is null.");
            }
            var member = await _context.Members.FindAsync(id);
            if (member != null)
            {
                _context.Members.Remove(member);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // GET: Member/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Members == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(t => t.Household)
                .FirstOrDefaultAsync(m => m.MemberId == id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        [NonAction]
        public void PopulateHouseholds()
        {
            var HouseholdCollection = _context.Households.ToList();
            Household DefaultHousehold = new Household() { HouseholdId = 0, HouseholdName = "Choose a Household" };
            HouseholdCollection.Insert(0, DefaultHousehold);
            ViewBag.Households = HouseholdCollection;
        }

        [NonAction]
        public void PopulateIdentityUsers()
        {
            var UserCollection = _context.IdentityUsers.ToList();
            IdentityUser DefaultUser = new IdentityUser() { Id = "", UserName = "Choose an Identity User"};
            UserCollection.Insert(0, DefaultUser);
            ViewBag.IdentityUsers = UserCollection;
        }

        [NonAction]
        public async Task PopulateIcons()
        {
            IconRequestor req = new IconRequestor();
            List<Icon> icons = await req.GetIconsFromApi();
            ViewBag.Icons = icons;
        }

    }
}
