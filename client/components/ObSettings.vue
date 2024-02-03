<template>
    <header class="border-b-2 border-white text-sm font-mono text-gray-300">
        <div class="flex">

            <NuxtLink to="/" class="my-1 font-bold text-md p-1 mx-3 cursor-pointer hover:shadow-white border-sm">
                ❮
            </NuxtLink>

            <div class="justify-center relative text-gray-500">
                <div class="">
                    <input type="text" v-model="selectedSymbol" v-on:input="fetchSymbols" v-on:blur="hideSuggestions"
                        v-on:keydown.enter="hideSuggestions"
                        class="select-all p-2 rounded-none focus:border-gray-200 my-1 font-bold p-1 text-white bg-black text-md mr-10 uppercase" />
                </div>
                <ul class="bg-white text-center absolute mt-2 z-40">
                    <li v-for="sugg in symbols" :key="sugg"
                        class="p-2 py-1 bg-black hover:bg-gray-50">
                        {{ sugg }}
                    </li>
                </ul>
            </div>

            <div class="flex items-center space-x-4">
                
                <div class="text-white">
                    Width:
                    <input v-model="width" min="10" step="1" type="number"
                        class="max-w-16 text-center border-none bg-transparent text-white focus:outline-none focus:ring-0" />
                </div>

                <div class="text-white">
                    Height:
                    <input v-model="height" min="10" step="2" type="number"
                        class="max-w-16 text-center bg-transparent text-white focus:outline-none focus:ring-0" />
                </div>

                <div class="text-white">
                    Tick Size:
                    <input readonly v-model="minTick" step="0.001" type="number"
                        class="max-w-16 text-center bg-transparent text-white focus:outline-none focus:ring-0" />
                </div>
            </div>

            <div class="flex items-center space-x-2 block ml-auto mx-5">
                <div v-show="showConn">
                    {{ getConnString() }}
                </div>
                <div class="w-4 h-4 rounded-full right-0"
                    :class="{ 'bg-green-500': isConnected(), 'bg-red-500': isDisconnected(), 'bg-orange-500': isConnecting() }"
                    @mouseover="showConn = true" @mouseleave="showConn = false"></div>

            </div>
        </div>
    </header>
</template> 

<script lang="ts" setup>
import ConnectionState from './types/ConnectionState';

const width = useState("width");
const height = useState("height")

const connState = useState<ConnectionState>('connectionState');
const minTick = useState('tickSize');

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
  
<script lang="ts">

export default {
    data() {
        
        const { SERVER_URL } = useRuntimeConfig().public;
        return {
            showConn: false,
            selectedSymbol: "ordiusdt",
            symbols: new Array<string>(),
            SERVER_URL: SERVER_URL
        };
    },
    methods: {
        async fetchSymbols() {
            const apiUrl = `${this.SERVER_URL}/symbols?name=${this.selectedSymbol}`;

            try {
                const response = await fetch(apiUrl);
                const data = await response.json();

                this.trySelect(data);
            } catch (error) {
                console.error('Error fetching symbols:', error);
            }
        },
        trySelect(data: string[]) {
            if (!data) {
                this.symbols = [];
                return;
            }

            if (data.findIndex((v) => v == this.selectedSymbol.toLowerCase()) !== -1) {
                const symbol = useState('symbol');
                symbol.value = this.selectedSymbol;
            }

            this.symbols = data;
        },
        hideSuggestions() {
            this.symbols = []
        }
    }
}
</script>