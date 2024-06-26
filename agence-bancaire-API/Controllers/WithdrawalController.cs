﻿using agence_bancaire_API.DTO;
using agence_bancaire_Business_Layer;
using agence_bancaire_DataAccess_Layer;
using Microsoft.AspNetCore.Mvc;

namespace agence_bancaire_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WithdrawalController : Controller
    {
        private static readonly object _lock = new object();

        [HttpPost]
        public async Task<IActionResult> Withdrawal([FromBody] DepositDTO request)
        {
            clsWithdrawal _Withdrawal = new clsWithdrawal();
            clsCheckingAccount _CheckingAccount = clsCheckingAccount.Find(request.checkingaccount_id);

            if (_CheckingAccount == null)
            {
                return NotFound();
            }

            int AppliedFees = (_CheckingAccount.Balance > request.amount) ? 0 : _CheckingAccount.CanApplyOverdraft(request.amount) ? 
                    clsCheckingAccount.CalculateOverdraftFee() : -1;  

            if (AppliedFees != -1)
            {
                _Withdrawal.amount = request.amount + AppliedFees;
                _Withdrawal.checkingaccount_id = request.checkingaccount_id;
                _Withdrawal.date_operation = DateTime.Now;
                _Withdrawal.LevelID = request.LevelID;

                var tasks = new List<Task<bool>>();

                lock (_lock)
                {
                    tasks.Add(Task.Run(async () => await _Withdrawal.Save()));
                }

                try
                {
                    var results = await Task.WhenAll(tasks);

                    if (Array.TrueForAll(results, result => result))
                    {
                        return CreatedAtAction(nameof(Withdrawal), null);
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to Withdrawal Money. Internal server error occurred.");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"{ex}");
                }

                //try
                //{
                //    if (await _Withdrawal.Save())
                //    {
                //        return CreatedAtAction(nameof(Withdrawal), null);
                //    }
                //    else
                //    {
                //        return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to Withdrawal Money. Internal server error occurred.");
                //    }

                //}
                //catch (Exception ex)
                //{
                //    return StatusCode(StatusCodes.Status500InternalServerError, $"{ex}");
                //}


            }
            else
            {
                return BadRequest("Insufficient funds including overdraft limit.");
            }

        }
    }
}
