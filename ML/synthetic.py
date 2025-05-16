import matplotlib.pyplot as plt
from huggingface_hub import InferenceClient
from PIL import Image
import base64
from system_prompt import system_prompt
from io import BytesIO
import os
from tqdm import tqdm
import json
import requests
import time

client = InferenceClient(
    provider="hyperbolic",
    api_key="<token>",
)
dataset = []
for img in tqdm(os.listdir('graphics')):
    try:

        image_path = f"graphics/{img}"
        image = Image.open(image_path)
        image = image.resize((1080, 600))

        buffered = BytesIO()
        image.save(buffered, format="PNG")
        img_base64 = base64.b64encode(buffered.getvalue()).decode("utf-8")

        response = requests.post(
            url="https://openrouter.ai/api/v1/chat/completions",
            headers={
                "Authorization": "Bearer <token>",
                "Content-Type": "application/json",
            },
            data=json.dumps({
                "model": "qwen/qwen2.5-vl-72b-instruct:free",
                "messages": [{
                    "role": "system",
                    "content": [
                        {"type": "text", "text": system_prompt},
                    ]
                },
                {
                    "role": "user",
                    "content": [
                        {
                            "type": "image_url",
                            "image_url": {
                                "url": f"data:image/png;base64,{img_base64}"
                            }
                        }
                    ]
                }
            ],
        })
        )
        response = response.json()['choices'][0]['message']['content']

        dataset.append({'id': img.split('_')[-1].replace('.png', ''), 'text':response})
        with open('dataset.json', 'w', encoding='utf-8') as f:
            json.dump(dataset, f, ensure_ascii=False, indent=4)
    except:
        time.sleep(30)
