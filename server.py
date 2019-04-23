#!/usr/bin/python -u

import subprocess
import os
import io
import uuid
import sys
from datetime import datetime
import base64
from flask import Flask, url_for, request, jsonify, send_file

os.chdir(os.path.dirname(os.path.abspath(__file__)))

app = Flask(__name__)

DIR_OUTPUT = "output"

def ensureFolderExists(folder):
    if not os.path.exists(folder):
        os.makedirs(folder)

def datePath():
    return datetime.now().strftime("%Y%m%d")
	
@app.route('/', methods=['GET'])
def hello():
	return "PKSM"

@app.route('/api/legalize', methods = ['POST'])
def api_legalize():
    try:
        if request.headers['Content-Type'] == 'application/base64':
            if not request.data or not request.headers.get('Version') or not request.headers.get("Size"):
                print("Invalid request made!")
                return None
            storeFolder = os.path.join(DIR_OUTPUT, datePath())
            ensureFolderExists(storeFolder)
            uniqint = uuid.uuid4()
            inputPath = os.path.join(storeFolder, str(uniqint) + ".pkx")
            with open(inputPath, 'wb') as f:
                f.write(base64.b64decode(request.data)[:int(request.headers['Size'])])
            if os.name == 'nt':
                proc = subprocess.Popen(['CoreConsole.exe', '-alm', '--version', request.headers.get('Version'), '-i', inputPath, '-o', inputPath], stdout=subprocess.PIPE, shell=True)
            else:
                proc = subprocess.Popen('mono CoreConsole.exe -alm --version {} -i "{}" -o "{}"'.format(request.headers.get('Version'), inputPath, inputPath), stdout=subprocess.PIPE, shell=True)
            (out, err) = proc.communicate()
            fname = str(uniqint) + '.pkx'
            with open(inputPath, 'rb') as f:
                pkx = f.read()
            return send_file(io.BytesIO(pkx), attachment_filename=fname)
    except Exception as e:
        print(e)
        return None

if __name__ == '__main__':
    ensureFolderExists(DIR_OUTPUT)
    app.run(host='0.0.0.0', port=4001)
