<template>
    <header class="border-b-2 border-white text-sm font-mono text-gray-300">
        <div class="flex">
            <!-- Back Button -->
            <NuxtLink to="/" class="my-1 font-bold text-md p-1 mx-3 cursor-pointer hover:shadow-white border-sm">
                ❮
            </NuxtLink>
            <!-- Symbol -->
            <input class="my-1 font-bold p-1 text-white bg-black text-md mr-10 uppercase" v-model="symbol"
                v-on:keyup.enter="symbolChange" />

            <!-- Inputs and Connection Status -->
            <div class="flex items-center space-x-4">
                <!-- Width Input -->
                <div class="text-white">
                    Width:
                    <input v-model="width" min="10" step="1" type="number"
                        class="max-w-16 text-center border-none bg-transparent text-white focus:outline-none focus:ring-0" />
                </div>

                <!-- Height Input -->
                <div class="text-white">
                    Height:
                    <input v-model="height" min="10" step="2" type="number"
                        class="max-w-16 text-center bg-transparent text-white focus:outline-none focus:ring-0" />
                </div>

                <!-- Ticksize Input -->
                <div class="text-white">
                    Tick Size:
                    <input readonly v-model="minTick" step="0.001" type="number"
                        class="max-w-16 text-center bg-transparent text-white focus:outline-none focus:ring-0" />
                </div>
            </div>


            <!-- Connection Status -->
            <div class="flex items-center space-x-2 block ml-auto mx-5">
                <div v-show="showConnectionStatus">
                    {{ getConnString() }}
                </div>
                <div class="w-4 h-4 rounded-full right-0"
                    :class="{ 'bg-green-500': isConnected(), 'bg-red-500': isDisconnected(), 'bg-orange-500': isConnecting() }"
                    @mouseover="showConnectionStatus = true" @mouseleave="showConnectionStatus = false"></div>

            </div>
        </div>
    </header>
</template> 
  
<script lang="ts">

export default {
    data() {
        return {
            // width: 0,
            // height: 0,
            // ticksize: 0,
            // isConnected: true,
            showConnectionStatus: false,
            selectedSymbol: "",
        };
    },
    methods: {
        symbolChange(__e: Event) {
            console.log(this.selectedSymbol)
            const symbol = useState("symbol");
            symbol.value = this.selectedSymbol;
        },
        onBeforeUpdate() {
            this.selectedSymbol = useState<string>("symbol").value;
        }
    }
}
</script>

<script lang="ts" setup>
import ConnectionState from './types/ConnectionState';

const width = useState("width");
const height = useState("height")

const connState = useState<ConnectionState>('connectionState');
let minTick = useState('tickSize');
const symbol = useState("symbol");

function isConnected() {
    return connState.value == ConnectionState.Connected;
}

function isConnecting() {
    return connState.value == ConnectionState.Connecting;
}

function isDisconnected() {
    return connState.value == ConnectionState.Disconnected;
}

function getConnString() {
    if (isConnected()) {
        return 'Connected'
    }
    else if (isConnecting()) {
        return 'Connecting'
    }
    else {
        return 'Disconnected';
    }
}

</script>