import dotenv from 'dotenv';
dotenv.config();

import colors from 'colors';
import fs from 'fs';
import path from 'path';
import moment from 'moment';
import axios from 'axios';
import figlet from 'figlet';
import chalk from 'chalk';
import gradient from 'gradient-string';
import ora from 'ora';
import boxen from 'boxen';
import cliProgress from 'cli-progress';
import pkg from 'terminal-kit';
const { terminal: terminalKit } = pkg;

import { delay } from './src/utils.js';
import Queue from 'queue';

import {
  getToken,
  getUsername,
  getBalance,
  getTribe,
  claimFarmReward,
  startFarmingSession,
  getTasks,
  claimTaskReward,
  getGameId,
  claimGamePoints,
  startTask,
  claimDailyReward,
  verifyTask,
} from './src/api.js';

const __filename = new URL(import.meta.url).pathname;
const __dirname = path.dirname(__filename);

const TOKEN_FILE_PATH = './TOKEN/accessTokens.txt';

const accountTokens = [
  process.env.QUERY_ID1,
  process.env.QUERY_ID2,
  process.env.QUERY_ID3,
  process.env.QUERY_ID4,
  process.env.QUERY_ID5,
  process.env.QUERY_ID6,
  process.env.QUERY_ID7,
  process.env.QUERY_ID8,
  process.env.QUERY_ID9,
  process.env.QUERY_ID10
];

const displayHeader = () => {
  console.clear();
  terminalKit.fullscreen();
  
  const title = gradient('cyan', 'magenta').multiline(figlet.textSync('BLUM BOT', {
    font: 'ANSI Shadow',
    horizontalLayout: 'fitted',
    verticalLayout: 'fitted',
  }));

  terminalKit.moveTo(1, 1);
  terminalKit(title);

  const subTitle = chalk.cyan('🌌 Advanced Multi-Account Airdrop System v2.0');
  terminalKit.moveTo(Math.floor((process.stdout.columns - subTitle.length) / 2), title.split('\n').length + 2);
  terminalKit(subTitle + '\n\n');

  const connInfo = chalk.magenta('📡 Secure Channel: t.me/slyntherinnn');
  terminalKit.moveTo(Math.floor((process.stdout.columns - connInfo.length) / 2), terminalKit.height - 2);
  terminalKit(connInfo);
};

const initializeProgressBars = (total) => {
  const multibar = new cliProgress.MultiBar({
    clearOnComplete: false,
    hideCursor: true,
    format: ' {bar} | {percentage}% | {value}/{total} Accounts',
  }, cliProgress.Presets.shades_grey);

  const overallBar = multibar.create(total, 0, { title: chalk.cyan('Overall Progress') });
  const taskBar = multibar.create(5, 0, { title: chalk.yellow('Current Task   ') });

  return { multibar, overallBar, taskBar };
};

const displayTaskProgress = (taskBar, taskName) => {
  taskBar.update(0, { title: chalk.yellow(`${taskName.padEnd(15)}`) });
  for (let i = 0; i <= 5; i++) {
    setTimeout(() => taskBar.update(i), i * 200);
  }
};

const displaySummary = (results) => {
  terminalKit.clear();
  terminalKit.moveTo(1, 1);

  const summaryTitle = gradient('gold', 'yellow').multiline(figlet.textSync('MISSION REPORT', {
    font: 'Small',
    horizontalLayout: 'fitted',
  }));
  terminalKit(summaryTitle + '\n\n');

  const successCount = results.filter((r) => r.success).length;
  const failCount = results.length - successCount;

  terminalKit.table(
    [
      ['Status', 'Count', 'Percentage'],
      ['Success', successCount, `${((successCount / results.length) * 100).toFixed(2)}%`],
      ['Fail', failCount, `${((failCount / results.length) * 100).toFixed(2)}%`]
    ],
    {
      hasBorder: true,
      contentHasMarkup: true,
      borderChars: 'lightRounded',
      borderAttr: { color: 'cyan' },
      width: 60,
      fit: true
    }
  );

  terminalKit.moveTo(1, terminalKit.height - 3);
  terminalKit(gradient.passion(`🎉 Mission Complete: ${successCount}/${results.length} accounts successfully processed!`));
};

const getTokenAndSave = async (queryId) => {
  const token = await getToken(queryId);
  if (token) {
    fs.writeFileSync(TOKEN_FILE_PATH, token);
    console.log('✅ New token has been saved.');
  }
  return token;
};

const handleApiError = async (error, retryCount = 0) => {
  const maxRetries = 3;

  if (error.response && error.response.data) {
    const message = error.response.data.message;

    if (message === `It's too early to claim`) {
      console.error(`🚨 Claim failed! It's too early to claim.`.red);
    } else if (message === 'Need to start farm') {
      console.error(`🚨 Claim failed! You need to start farm first.`.red);
    } else if (message === 'Need to claim farm') {
      console.error(`🚨 Claim failed! You need to claim farm first.`.red);
    } else if (message === 'Token expired') {
      console.error(`🚨 Token expired! Refreshing the token...`.red);
      await delay(3000);
      const newToken = await getTokenAndSave();
      return newToken;
    } else if (error.response.status === 520) {
      console.error(`🚨 Error 520: Cloudflare issue detected. Retrying... (${retryCount + 1}/${maxRetries})`.yellow);
      if (retryCount < maxRetries) {
        await delay(5000); 
        return retryCount + 1;
      } else {
        console.error(`❌ Gagal setelah ${maxRetries} kali mencoba.`.red);
        return null;
      }
    } else {
      console.error(`🚨 An unexpected error occurred: ${message}`.red);
    }
  } else {
    console.error(`🚨 An unexpected error occurred: ${error.message}`.red);
  }

  return null;
};

const performActionWithRetry = async (action, maxRetries = 3, timeout = 10000) => {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await Promise.race([
        action(),
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('Timeout')), timeout)
        )
      ]);
    } catch (error) {
      if (error.message === 'Timeout' || error.code === 'ECONNABORTED') {
        console.log(`Timeout occurred. Retrying... (${i + 1}/${maxRetries})`.yellow);
      } else if (error.response && error.response.status === 401) {
        console.error('🚨 Token expired or unauthorized. Please check your token.'.red);
        break;
      } else {
        console.log(`Error: ${error.message}. Retrying... (${i + 1}/${maxRetries})`.red);
      }
      
      if (i === maxRetries - 1) throw error;
      await delay(5000);
    }
  }
};

const retryAction = async (action, maxRetries = 3) => {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await action();
    } catch (error) {
      console.log(`❌ Error: ${error.message} (Retry ${i + 1}/${maxRetries})`.yellow);
      if (i === maxRetries - 1) throw error; 
      await delay(3000); 
    }
  }
};

const claimFarmRewardSafely = async (token) => {
  try {
    console.log('🌾 Attempting to claim farm reward...'.yellow);
    const result = await claimFarmReward(token);
    if (result && result.skipped) {
      console.log(`⏳ Farm reward claim skipped: ${result.reason}`.yellow);
    } else if (result) {
      console.log('✅ Farm reward claimed successfully!'.green);
    } else {
      console.log('⚠️ Farm reward claim returned no result.'.yellow);
    }
  } catch (error) {
    console.log(`❌ Failed to claim farm reward: ${error.message}`.red);
  }
};


const startFarmingSessionSafely = async (token) => {
  try {
    console.log('🚜 Attempting to start farming session...'.yellow);
    const farmingSession = await performActionWithRetry(() => startFarmingSession(token));
    
    if (farmingSession) {
      console.log(`✅ Farming session started!`.green);
      console.log(`Session details: ${JSON.stringify(farmingSession)}`.cyan);
    } else {
      console.log(`⚠️ Farming session start returned no result.`.yellow);
    }
  } catch (error) {
    console.log(`❌ Failed to start farming session: ${error.message}`.red);
    if (error.response) {
      console.log(`Response status: ${error.response.status}`.red);
      console.log(`Response data: ${JSON.stringify(error.response.data)}`.red);
    }
  }
};

const predefinedAnswers = {
  "What Are AMMs?": "CRYPTOSMART",
  "Say No to Rug Pull!": "SUPERBLUM",
  "What are Telegram Mini Apps?": "CRYPTOBLUM",
  "Navigating Crypto": "HEYBLUM",
  "Secure your Crypto!": "BEST PROJECT EVER",
  "Forks Explained": "GO GET",
  "How to Analyze Crypto?": "VALUE",
  "Liquidity Pools Guide": "BLUMERSSS"
};

const completeTasksSafely = async (token) => {
  try {
    console.log('✅ Auto completing tasks...'.yellow);

    const tasks = await getTasks(token);

    if (!Array.isArray(tasks) || tasks.length === 0) {
      console.log('⚠️ No tasks available or task data is in unexpected format.'.yellow);
      return;
    }

    const notStartedTasks = tasks.filter(task => task.status === "NOT_STARTED");
    const readyForVerifyTasks = tasks.filter(task => task.status === "READY_FOR_VERIFY");
    const inProgressTasks = tasks.filter(task => task.status === "IN_PROGRESS");

    console.log(`Not started tasks: ${notStartedTasks.length}`);
    console.log(`Ready for verify tasks: ${readyForVerifyTasks.length}`);
    console.log(`In progress tasks: ${inProgressTasks.length}`);

    for (const task of notStartedTasks) {
      if (task.title === 'Farm' || task.title === 'Invite') {
        console.log(`Skipping task: ${task.title}`.yellow);
        continue;
      }

      await startTask(token, task.id, task.title);
      console.log(`Started task: ${task.title}`.green);
      await delay(5000);

      if (predefinedAnswers[task.title]) {
        await submitAnswer(token, task.id, predefinedAnswers[task.title]);
        console.log(`Submitted answer for task: ${task.title}`.green);
      }
    }

    for (const task of readyForVerifyTasks) {
      const keyword = predefinedAnswers[task.title];
      console.log(`Validating task "${task.title}" with keyword: "${keyword}"`);

      if (keyword) {
        try {
          const result = await verifyTask(token, task.id, task.title, keyword);

          if (result) {
            console.log(`✅ Task "${task.title}" verified successfully.`);
          } else {
            console.log(`⚠️ Task "${task.title}" did not return a valid result.`);
          }
        } catch (error) {
          if (error.response?.data?.message === 'Task is not validating') {
            console.log(`❌ Task "${task.title}" cannot be validated. Skipping...`);
          } else {
            console.error(`❌ Error validating task "${task.title}": ${error.message}`);
          }
        }
      } else {
        console.log(`⚠️ No keyword found for task: ${task.title}`);
      }

      await delay(5000); 
    }

    for (const task of [...notStartedTasks, ...readyForVerifyTasks, ...inProgressTasks]) {
      const claimResult = await claimTaskReward(token, task.id, task.title);
      if (claimResult) {
        console.log(`Claimed reward for task: ${task.title}`.green);
      } else {
        console.log(`Unable to claim reward for task: ${task.title}. It may not be ready yet.`.yellow);
      }
      await delay(2000); 
    }

  } catch (error) {
    console.log(`❌ Failed to complete tasks: ${error.message}`.red);
  }
};

const getTaskStatus = async (token, taskId) => {
  const response = await axios.get(`https://earn-domain.blum.codes/api/v1/tasks/${taskId}`, {
    headers: { Authorization: token },
    timeout: 10000, 
  });
  return response.data;
};

const claimDailyRewardSafely = async (token) => {
  try {
    console.log('✨ Attempting to claim daily reward...'.yellow);
    const result = await claimDailyReward(token);
    if (result && result.skipped) {
      console.log(`⏳ Daily reward claim skipped: ${result.reason}`.yellow);
    } else if (result) {
      console.log('✅ Daily reward claimed successfully!'.green);
      console.log(`Reward details: ${JSON.stringify(result)}`.cyan);
    } else {
      console.log('⚠️ Daily reward claim returned no result.'.yellow);
    }
  } catch (error) {
    console.log(`❌ Failed to claim daily reward: ${error.message}`.red);
  }
};

const playAndClaimGameWithRetry = async (token, points, iteration, maxRetries = 4) => {
  let attempt = 0;
  let retryDelay = 13000; 

  while (attempt < maxRetries) {
    try {
      console.log(`🆔 Starting game ${iteration}...`.cyan);

      let playResponse = await axios.post('https://game-domain.blum.codes/api/v1/game/play', null, {
        headers: { Authorization: token, 'Content-Type': 'application/json' },
        timeout: 15000, 
      });

      let gameId = playResponse.data.gameId;
      console.log(`Game ID: ${gameId} (Iteration ${iteration})`.cyan);

      console.log(`⏳ Waiting for 32 seconds... (Iteration ${iteration})`.yellow);
      await delay(32000);

      let claimResponse = await axios.post(
        'https://game-domain.blum.codes/api/v1/game/claim',
        { gameId, points },
        { 
          headers: { Authorization: token, 'Content-Type': 'application/json' },
          timeout: 15000, 
        }
      );

      console.log(`✅ Successfully claimed ${points} points (Iteration ${iteration})`.green);
      return 'OK';
    } catch (error) {
      console.log(`❌ Failed to claim points for game ${iteration}: ${error.message}`.red);
      attempt++;

      if (error.response && error.response.status === 520) {
        console.log(`🔄 Server error (520). Retrying in ${retryDelay / 1000} seconds... (Attempt ${attempt}/${maxRetries})`.yellow);
      } else if (attempt < maxRetries) {
        console.log(`Retrying in ${retryDelay / 1000} seconds... (Attempt ${attempt}/${maxRetries})`.yellow);
      } else {
        console.log(`❌ Failed after ${maxRetries} attempts. Moving to next task.`.red);
        throw error;
      }

      await delay(retryDelay);
    }
  }
};

const claimGamePointsSafely = async (token) => {
  console.log('🎮 Starting game points claiming...'.cyan);

  const maxRetries = 3;
  const retryDelay = 5000; 

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      const balanceResponse = await getBalance(token);
      const gameChances = balanceResponse.playPasses;
      console.log(`📊 You have ${gameChances} game chances available.`.cyan);

      if (gameChances <= 0) {
        console.log('❌ No game chances available. Skipping game points claiming.'.yellow);
        return;
      }

      const repetitions = gameChances;
      const batchSize = 3;

      let tasks = [];
      for (let i = 0; i < repetitions; i += batchSize) {
        for (let j = 0; j < batchSize && (i + j) < repetitions; j++) {
          const randomPoints = Math.floor(Math.random() * (255 - 200 + 1)) + 200; 
          console.log(`Iteration ${i + j + 1}: Random points = ${randomPoints}`);
          tasks.push(playAndClaimGameWithRetry(token, randomPoints, i + j + 1));
        }

        let results = await Promise.all(tasks);  
        results.forEach((result, index) => {
          console.log(`Result of game ${i + index + 1}:`, result);
        });

        await delay(1000);  
      }

      console.log('🏁 All games have been played.'.green);
      return; 
    } catch (error) {
      console.log(`❌ Attempt ${attempt + 1}/${maxRetries} failed: ${error.message}`.red);
      if (error.response && error.response.status === 520) {
        console.log(`🔄 Server error (520). Retrying in ${retryDelay / 1000} seconds...`.yellow);
        await delay(retryDelay);
      } else {
        throw error; 
      }
    }
  }

  console.log(`❌ Failed to process game points claiming after ${maxRetries} attempts.`.red);
};

const processAccount = async (queryId, taskBar) => {
  let token = await getTokenAndSave(queryId);

  if (!token) {
    console.error(chalk.red('✖ [ERROR] Token is undefined! Skipping this account.'));
    return { success: false, queryId, error: 'Token undefined' };
  }

  try {
    console.log(chalk.cyan(`\n🔄 Processing account with queryId: ${queryId}`));

    displayTaskProgress(taskBar, 'Claiming Farm');
    await claimFarmRewardSafely(token);
    console.log(chalk.green('✅ Farm claim process completed'));
    
    displayTaskProgress(taskBar, 'Farming Session');
    await startFarmingSessionSafely(token);
    console.log(chalk.green('✅ Farming session process completed'));
    
    displayTaskProgress(taskBar, 'Daily Reward');
    await claimDailyRewardSafely(token);
    console.log(chalk.green('✅ Daily reward process completed'));
    
    displayTaskProgress(taskBar, 'Auto Tasks');
    await completeTasksSafely(token);
    console.log(chalk.green('✅ Auto tasks process completed'));
    
    displayTaskProgress(taskBar, 'Game Points');
    await claimGamePointsSafely(token);
    console.log(chalk.green('✅ Game points process completed'));

    console.log(chalk.green(`✅ All processes completed for account with queryId: ${queryId}`));

    await delay(10000); 
    return { success: true, queryId };
  } catch (error) {
    console.error(chalk.red(`✖ [FAILURE] Error occurred for queryId: ${queryId} - ${error.message}`));
    return { success: false, queryId, error: error.message };
  }
};

const runScriptForAllAccounts = async () => {
  displayHeader();

  const results = [];
  const total = accountTokens.length;
  const { multibar, overallBar, taskBar } = initializeProgressBars(total);

  for (let i = 0; i < accountTokens.length; i++) {
    const queryId = accountTokens[i];
    
    const result = await processAccount(queryId, taskBar);
    results.push(result);

    overallBar.update(i + 1);
  }

  multibar.stop();
  await delay(1000);
  displaySummary(results);
};

runScriptForAllAccounts();
