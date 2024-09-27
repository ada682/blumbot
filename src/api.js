import axios from 'axios';
import axiosRetry from 'axios-retry';

axiosRetry(axios, {
  retries: 3,
  retryDelay: axiosRetry.exponentialDelay,
  retryCondition: (error) => {
    return axiosRetry.isNetworkOrIdempotentRequestError(error) || error.code === 'ECONNABORTED';
  }
});

const API_TIMEOUT = 30000; 

export async function getToken(queryId) {
  try {
    const { data } = await axios({
      url: 'https://user-domain.blum.codes/api/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP',
      method: 'POST',
      data: {
        query: queryId,
        referralToken: '554eWV40LM',
      },
      timeout: API_TIMEOUT,
    });

    if (data && data.token && data.token.access) {
      console.log('✅ Token successfully retrieved.');
      return `Bearer ${data.token.access}`;
    } else {
      console.error('❌ Failed to retrieve a valid token.');
      return null;
    }
  } catch (error) {
    console.error(`❌ Error occurred while fetching token: ${error.message}`);
    return null;
  }
}

export async function getUsername(token) {
  const response = await axios({
    url: 'https://gateway.blum.codes/v1/user/me',
    method: 'GET',
    headers: { Authorization: token },
    timeout: API_TIMEOUT,
  });
  return response.data.username;
}

export async function getBalance(token) {
  const response = await axios({
    url: 'https://game-domain.blum.codes/api/v1/user/balance',
    method: 'GET',
    headers: { Authorization: token },
    timeout: API_TIMEOUT,
  });
  return response.data;
}

export async function getTribe(token) {
  try {
    const response = await axios({
      url: 'https://tribe-domain.blum.codes/api/v1/tribe/my',
      method: 'GET',
      headers: { Authorization: token },
      timeout: API_TIMEOUT,
    });
    return response.data;
  } catch (error) {
    if (error.response && error.response.data && error.response.data.message === 'NOT_FOUND') {
      return null;
    } else {
      console.log(error.response ? error.response.data.message : error.message);
      return null;
    }
  }
}

export async function claimFarmReward(token) {
  try {
    console.log('Sending farm reward claim request...'.cyan);
    const { data } = await axios({
      url: 'https://game-domain.blum.codes/api/v1/farming/claim',
      method: 'POST',
      headers: { Authorization: token },
      data: null,
      timeout: API_TIMEOUT,
    });
    
    console.log(`Farm claim response: ${JSON.stringify(data)}`.cyan);
    
    if (data && data.success) {
      console.log('✅ Farm reward claimed successfully!'.green);
      return data;
    } else {
      console.error('❌ Farm claim failed: Unexpected response'.red);
      return null;
    }
  } catch (error) {
    if (error.response && error.response.data) {
      if (error.response.data.message === "It's too early to claim") {
        console.log(`⏳ It's too early to claim farm reward. Skipping...`.yellow);
        return { skipped: true, reason: "too_early" };
      } else {
        console.error(`❌ Farm claim failed: ${error.response.data.message}`.red);
      }
      console.error(`Full error response: ${JSON.stringify(error.response.data)}`.red);
    } else {
      console.error(`❌ Error occurred during farm claim: ${error.message}`.red);
    }
    return null;
  }
}

export async function claimDailyReward(token) {
  try {
    console.log('Sending daily reward claim request...'.cyan);
    const { data } = await axios({
      url: 'https://game-domain.blum.codes/api/v1/daily-reward?offset=-420',
      method: 'POST',
      headers: {
        Authorization: token,
      },
      data: null,
      timeout: API_TIMEOUT,
    });

    console.log(`Daily reward claim response: ${JSON.stringify(data)}`.cyan);
    return data;
  } catch (error) {
    if (error.response && error.response.data) {
      if (error.response.data.message === 'same day') {
        console.log(`⏳ Daily reward already claimed today. Skipping...`.yellow);
        return { skipped: true, reason: "already_claimed" };
      } else {
        console.error(`❌ Daily claim failed: ${error.response.data.message}`.red);
      }
      console.error(`Full error response: ${JSON.stringify(error.response.data)}`.red);
    } else {
      console.error(`❌ Error occurred during daily claim: ${error.message}`.red);
    }
    return null;
  }
}

export async function startFarmingSession(token) {
  const { data } = await axios({
    url: 'https://game-domain.blum.codes/api/v1/farming/start',
    method: 'POST',
    headers: { Authorization: token },
    data: null,
    timeout: API_TIMEOUT,
  });
  return data;
}

export async function getTasks(token) {
     try {
       const { data } = await axios({
         url: 'https://earn-domain.blum.codes/api/v1/tasks',
         method: 'GET',
         headers: { Authorization: token },
         timeout: API_TIMEOUT,
       });

       if (Array.isArray(data)) {
         let allTasks = data.flatMap(section =>
           (section.tasks || []).concat(
             section.subSections?.flatMap(subsection => subsection.tasks || []) || []
           )
         );

         allTasks.forEach(task => {
           console.log(`Task: ${task.title}, Status: ${task.status}, ID: ${task.id}`);
         });

         return allTasks;
       } else {
         console.error('❌ Unexpected task data structure'.red);
         return [];
       }
     } catch (error) {
       console.error(`❌ Error fetching tasks: ${error.message}`.red);
       return [];
     }
   }

export async function startTask(token, taskId, title) {
     try {
       const { data } = await axios({
         url: `https://earn-domain.blum.codes/api/v1/tasks/${taskId}/start`,
         method: 'POST',
         headers: { Authorization: token },
         data: null,
         timeout: API_TIMEOUT,
       });
       
       console.log(`✅ Task "${title}" started successfully`.green);
       return data;
     } catch (error) {
       if (error.response && error.response.data) {
         console.error(`❌ Error starting task "${title}": ${error.response.data.message}`.red);
         if (error.response.status === 400 && error.response.data.message.includes('already started')) {
           console.log(`ℹ️ Task "${title}" was already started`.yellow);
           return { alreadyStarted: true };
         }
       } else {
         console.error(`❌ Unexpected error starting task "${title}": ${error.message}`.red);
       }
       return null;
     }
   }

export async function verifyTask(token, taskId, title, keyword) {
  try {
    const { data } = await axios({
      url: `https://earn-domain.blum.codes/api/v1/tasks/${taskId}/validate`,
      method: 'POST',
      headers: { Authorization: token },
      data: { keyword }, // Pastikan keyword dikirim di sini
      timeout: API_TIMEOUT,
    });
    
    console.log(`✅ Task "${title}" validated successfully with keyword: ${keyword}`.green);
    return data;
  } catch (error) {
    if (error.response && error.response.data) {
      console.error(`❌ Error validating task "${title}": ${error.response.data.message}`.red);
    } else {
      console.error(`❌ Unexpected error validating task "${title}": ${error.message}`.red);
    }
    return null;
  }
}

export async function claimTaskReward(token, taskId, title) {
  try {
    console.log(`Attempting to claim reward for task: ${title}`.cyan);
    const { data } = await axios({
      url: `https://earn-domain.blum.codes/api/v1/tasks/${taskId}/claim`,
      method: 'POST',
      headers: { Authorization: token },
      data: null,
      timeout: API_TIMEOUT,
    });
    
    console.log(`✅ Reward for task "${title}" claimed successfully`.green);
    return data;
  } catch (error) {
    if (error.response?.data?.message === 'Task is not done') {
      console.log(`Task "${title}" is not ready for claim yet.`.yellow);
    } else {
      console.error(`Error claiming reward for task "${title}": ${error.message}`.red);
    }
    return null;
  }
}

export async function getGameId(token) {
  const { data } = await axios({
    url: 'https://game-domain.blum.codes/api/v1/game/play',
    method: 'POST',
    headers: { Authorization: token },
    data: null,
    timeout: API_TIMEOUT,
  });
  return data;
}

export async function claimGamePoints(token, gameId, points) {
  const { data } = await axios({
    url: `https://game-domain.blum.codes/api/v1/game/claim`,
    method: 'POST',
    headers: { Authorization: token },
    data: {
      gameId,
      points,
    },
    timeout: API_TIMEOUT,
  });
  return data;
}