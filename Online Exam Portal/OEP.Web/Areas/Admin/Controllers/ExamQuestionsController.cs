﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.UI;
using OEP.Core.DomainModels.ExamModels;
using OEP.Data;
using OEP.Core.Services;
using OEP.Resources.Admin;
using AutoMapper;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using OEP.Core.DomainModels.QuestionModel;
using OEP.Web.Helpers;

namespace OEP.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExamQuestionsController : Controller
    {
        private readonly IExamQuestionService _examQuestionService;
        private readonly IQuestionService _questionService;
        private readonly IExamservice _examservice;

        public ExamQuestionsController(IExamQuestionService examQuestionService, IQuestionService questionService, IExamservice examservice)
        {

            _examQuestionService = examQuestionService;
            _questionService = questionService;
            _examservice = examservice;
        }



        // GET: Admin/ExamQuestions/Create/5
        public async Task<ActionResult> Create(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var exam = await _examservice.GetByIdAsync(Convert.ToInt32(id));
            if (exam == null)
            {
                return HttpNotFound();
            }
            ExamQuestionViewModel examQuestionViewModel = new ExamQuestionViewModel();
            examQuestionViewModel.ExamId = exam.Id;
            examQuestionViewModel.ExamName = exam.Name;
            var examQuestion = await _examQuestionService.GetAllIncludingAsync(x => x.Questions);
            examQuestionViewModel.ExamQuestionResourceList = Mapper.Map<List<ExamQuestion>, List<ExamQuestionResource>>(examQuestion);

            return View(examQuestionViewModel);
        }



        [HttpGet]
        // GET: Admin/ExamQuestions/GetQuestions
        public async Task<ActionResult> GetQuestions(string phrase)
        {
            var questions = await _questionService.GetAllAsync();
            questions = questions.Where(x => x.Question.Contains(phrase) && x.Status).ToList();
            var questionRes = Mapper.Map<List<Questions>, List<QuestionAutoCompleteResource>>(questions);
            return Json(questionRes, JsonRequestBehavior.AllowGet);
        }



        // POST: Admin/ExamQuestions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<string> Create(ExamQuestionResource examQuestionResource)
        {
            if (ModelState.IsValid)
            {
                var examQuestionExist =
                    _examQuestionService.FindByAsync(
                        x => x.ExamId == examQuestionResource.ExamId && x.QuestionId == examQuestionResource.QuestionId);

                if (examQuestionExist.Result == null || !examQuestionExist.Result.Any())
                {
                    var examQuestion = Mapper.Map<ExamQuestionResource, ExamQuestion>(examQuestionResource);
                    examQuestion.CreatedDate = DateTime.Now;
                    examQuestion.UpdatedDate = DateTime.Now;
                    var userId = System.Web.HttpContext.Current.User.Identity.GetUserId();
                    examQuestion.UserId = userId;
                    examQuestion.Status = true;
                    await _examQuestionService.AddAsync(examQuestion);
                    _examQuestionService.UnitOfWorkSaveChanges();

                    var partialViewHtmlString = await ConvertListToString();
                    var response = JsonConvert.SerializeObject(new ResponseContent<string>() { Status = "Success", Message = "", Result = partialViewHtmlString });
                    return response;
                }
                else
                {
                    return JsonConvert.SerializeObject(new ResponseContent<string>() { Status = "Exist", Message = "The Question is already added!", Result = "" });
                }
            }
            return JsonConvert.SerializeObject(new ResponseContent<string>() { Status = "Error", Message = "The enter valid details!", Result = "" });
        }

        
        [HttpPost]
        public async Task<string> Delete(int? id)
        {
            if (id != null)
            {
                var examQuestionExist =await _examQuestionService.GetByIdAsync(Convert.ToInt32(id));
                if (examQuestionExist != null)
                {
                    await _examQuestionService.DeleteAsync(examQuestionExist);
                    var partialViewHtmlString = await ConvertListToString();
                    return JsonConvert.SerializeObject(new ResponseContent<string>() { Status = "Success", Message = "", Result = partialViewHtmlString });
                }
                return  JsonConvert.SerializeObject(new ResponseContent<string>() { Status = "NotExist", Message = "The Item doesn't exist!", Result = "" });
            }
            return JsonConvert.SerializeObject(new ResponseContent<string>() { Status = "Error", Message = "The enter valid details!", Result = "" });
        }

        private async Task<string> ConvertListToString()
        {
            var exmaQuestionList = await _examQuestionService.GetAllIncludingAsync(x => x.Questions);
            var examQuestionListResource = Mapper.Map<List<ExamQuestion>, List<ExamQuestionResource>>(exmaQuestionList);
            // convert examquestion model to resource
            string ret =
                PartialView("~/Areas/Admin/Views/ExamQuestions/ExamQuestionsList.cshtml", examQuestionListResource)
                    .RenderToString();
            return ret;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _examQuestionService.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}